// Copyright (c) Microsoft Corporation. All rights reserved.

#nullable enable

using System;
using System.IO;
using System.Security.Cryptography;

namespace Microsoft.DotNet.HotReload;

internal sealed class SharedSecretProvider : IDisposable
{
    private readonly RSA _rsa = RSA.Create(2048);

    public void Dispose()
        => _rsa.Dispose();

    internal string DecryptSecret(string secret)
        => Convert.ToBase64String(_rsa.Decrypt(Convert.FromBase64String(secret), RSAEncryptionPadding.OaepSHA256));

    internal string GetPublicKey()
#if NET
        => Convert.ToBase64String(_rsa.ExportSubjectPublicKeyInfo());
#else
        => ExportPublicKeyNetFramework();
#endif

    /// <summary>
    /// Export the public key in the X.509 SubjectPublicKeyInfo representation which is equivalent to the .NET Core RSA api
    /// ExportSubjectPublicKeyInfo.
    ///
    /// Algorithm from https://stackoverflow.com/a/28407693 or https://github.com/Azure/azure-powershell/blob/main/src/KeyVault/KeyVault/Helpers/JwkHelper.cs
    /// </summary>
    internal string ExportPublicKeyNetFramework()
    {
        var writer = new StringWriter();
        ExportPublicKey(ExportPublicKeyParameters(), writer);
        return writer.ToString();
    }

    internal RSAParameters ExportPublicKeyParameters()
        => _rsa.ExportParameters(includePrivateParameters: false);

    private static void ExportPublicKey(RSAParameters parameters, TextWriter outputStream)
    {
        if (parameters.Exponent == null || parameters.Modulus == null)
        {
            throw new ArgumentException($"{parameters} does not contain valid public key information");
        }

        using (var stream = new MemoryStream())
        {
            var writer = new BinaryWriter(stream);
            writer.Write((byte)0x30); // SEQUENCE
            using (var innerStream = new MemoryStream())
            {
                var innerWriter = new BinaryWriter(innerStream);
                innerWriter.Write((byte)0x30); // SEQUENCE
                EncodeLength(innerWriter, 13);
                innerWriter.Write((byte)0x06); // OBJECT IDENTIFIER
                var rsaEncryptionOid = new byte[] { 0x2a, 0x86, 0x48, 0x86, 0xf7, 0x0d, 0x01, 0x01, 0x01 };
                EncodeLength(innerWriter, rsaEncryptionOid.Length);
                innerWriter.Write(rsaEncryptionOid);
                innerWriter.Write((byte)0x05); // NULL
                EncodeLength(innerWriter, 0);
                innerWriter.Write((byte)0x03); // BIT STRING
                using (var bitStringStream = new MemoryStream())
                {
                    var bitStringWriter = new BinaryWriter(bitStringStream);
                    bitStringWriter.Write((byte)0x00); // # of unused bits
                    bitStringWriter.Write((byte)0x30); // SEQUENCE
                    using (var paramsStream = new MemoryStream())
                    {
                        var paramsWriter = new BinaryWriter(paramsStream);
                        EncodeIntegerBigEndian(paramsWriter, parameters.Modulus); // Modulus
                        EncodeIntegerBigEndian(paramsWriter, parameters.Exponent); // Exponent
                        var paramsLength = (int)paramsStream.Length;
                        EncodeLength(bitStringWriter, paramsLength);
                        bitStringWriter.Write(paramsStream.GetBuffer(), 0, paramsLength);
                    }
                    var bitStringLength = (int)bitStringStream.Length;
                    EncodeLength(innerWriter, bitStringLength);
                    innerWriter.Write(bitStringStream.GetBuffer(), 0, bitStringLength);
                }
                var length = (int)innerStream.Length;
                EncodeLength(writer, length);
                writer.Write(innerStream.GetBuffer(), 0, length);
            }

            var base64 = Convert.ToBase64String(stream.GetBuffer(), 0, (int)stream.Length).ToCharArray();
            for (var i = 0; i < base64.Length; i += 64)
            {
                outputStream.Write(base64, i, Math.Min(64, base64.Length - i));
            }
        }
    }

    private static void EncodeLength(BinaryWriter stream, int length)
    {
        if (length < 0) throw new ArgumentOutOfRangeException(nameof(length), "Length must be non-negative");
        if (length < 0x80)
        {
            // Short form
            stream.Write((byte)length);
        }
        else
        {
            // Long form
            var temp = length;
            var bytesRequired = 0;
            while (temp > 0)
            {
                temp >>= 8;
                bytesRequired++;
            }
            stream.Write((byte)(bytesRequired | 0x80));
            for (var i = bytesRequired - 1; i >= 0; i--)
            {
                stream.Write((byte)(length >> (8 * i) & 0xff));
            }
        }
    }

    private static void EncodeIntegerBigEndian(BinaryWriter stream, byte[] value, bool forceUnsigned = true)
    {
        stream.Write((byte)0x02); // INTEGER
        var prefixZeros = 0;
        for (var i = 0; i < value.Length; i++)
        {
            if (value[i] != 0) break;
            prefixZeros++;
        }
        if (value.Length - prefixZeros == 0)
        {
            EncodeLength(stream, 1);
            stream.Write((byte)0);
        }
        else
        {
            if (forceUnsigned && value[prefixZeros] > 0x7f)
            {
                // Add a prefix zero to force unsigned if the MSB is 1
                EncodeLength(stream, value.Length - prefixZeros + 1);
                stream.Write((byte)0);
            }
            else
            {
                EncodeLength(stream, value.Length - prefixZeros);
            }
            for (var i = prefixZeros; i < value.Length; i++)
            {
                stream.Write(value[i]);
            }
        }
    }
}
