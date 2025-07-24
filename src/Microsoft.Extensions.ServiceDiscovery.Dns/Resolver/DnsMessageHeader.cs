// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.ServiceDiscovery.Dns.Resolver;

// RFC 1035 4.1.1. Header section format
internal struct DnsMessageHeader
{
    internal const int HeaderLength = 12;
    public ushort TransactionId { get; set; }

    internal QueryFlags QueryFlags { get; set; }

    public ushort QueryCount { get; set; }

    public ushort AnswerCount { get; set; }

    public ushort AuthorityCount { get; set; }

    public ushort AdditionalRecordCount { get; set; }

    public QueryResponseCode ResponseCode
    {
        get => (QueryResponseCode)(QueryFlags & QueryFlags.ResponseCodeMask);
    }

    public bool IsResultTruncated
    {
        get => (QueryFlags & QueryFlags.ResultTruncated) != 0;
    }

    public bool IsResponse
    {
        get => (QueryFlags & QueryFlags.HasResponse) != 0;
    }
}
