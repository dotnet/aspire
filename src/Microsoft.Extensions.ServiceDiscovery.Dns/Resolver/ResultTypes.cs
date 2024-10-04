// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Text;

namespace Microsoft.Extensions.ServiceDiscovery.Dns.Resolver;

internal record struct AddressResult(DateTime ExpiresAt, IPAddress Address);

internal record struct ServiceResult(DateTime ExpiresAt, int Priority, int Weight, int Port, string Target, AddressResult[] Addresses);

internal record struct TxtResult(int Ttl, byte[] Data)
{
    internal IEnumerable<string> GetText() => GetText(Encoding.ASCII);

    internal IEnumerable<string> GetText(Encoding encoding)
    {
        for (int i = 0; i < Data.Length;)
        {
            int length = Data[i];
            yield return encoding.GetString(Data, i + 1, length);
            i += length + 1;
        }
    }
}
