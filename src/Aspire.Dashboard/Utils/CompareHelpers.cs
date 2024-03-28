// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Security.Cryptography;
using System.Text;

namespace Aspire.Dashboard.Utils;

internal static class CompareHelpers
{
    // This method is used to compare two keys in a way that avoids timing attacks.
    public static bool CompareKey(byte[] expectedKeyBytes, string requestKey)
    {
        const int StackAllocThreshold = 256;

        var requestByteCount = Encoding.UTF8.GetByteCount(requestKey);

        // Key will never match if lengths are different. But still do all the comparison work to avoid timing attacks.
        var lengthsEqual = expectedKeyBytes.Length == requestByteCount;

        var requestSpanLength = Math.Max(requestByteCount, expectedKeyBytes.Length);
        byte[]? requestPooled = null;
        var requestBytesSpan = (requestSpanLength <= StackAllocThreshold ?
            stackalloc byte[StackAllocThreshold] :
            (requestPooled = RentClearedArray(requestSpanLength))).Slice(0, requestSpanLength);

        try
        {
            // Always succeeds because the byte span is always as big or bigger than required.
            Encoding.UTF8.GetBytes(requestKey, requestBytesSpan);

            // Trim request bytes to the same length as expected bytes. Need to be the same size for fixed time comparison.
            var equals = CryptographicOperations.FixedTimeEquals(expectedKeyBytes, requestBytesSpan.Slice(0, expectedKeyBytes.Length));

            return equals && lengthsEqual;
        }
        finally
        {
            if (requestPooled != null)
            {
                ArrayPool<byte>.Shared.Return(requestPooled);
            }
        }

        static byte[] RentClearedArray(int byteCount)
        {
            // UTF8 bytes are copied into the array but remaining bytes are untouched.
            // Because all bytes in the array are compared, clear the array to avoid comparing previous data.
            var array = ArrayPool<byte>.Shared.Rent(byteCount);
            Array.Clear(array);
            return array;
        }
    }
}
