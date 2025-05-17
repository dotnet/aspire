// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.Extensions.ServiceDiscovery.Dns.Resolver;

internal sealed partial class DnsResolver : IDnsResolver, IDisposable
{
    private const int IPv4Length = 4;
    private const int IPv6Length = 16;

    // CancellationTokenSource.CancelAfter has a maximum timeout of Int32.MaxValue milliseconds.
    private static readonly TimeSpan s_maxTimeout = TimeSpan.FromMilliseconds(int.MaxValue);

    private bool _disposed;
    private readonly ResolverOptions _options;
    private readonly CancellationTokenSource _pendingRequestsCts = new();
    private TimeProvider _timeProvider = TimeProvider.System;
    private readonly ILogger<DnsResolver> _logger;

    internal void SetTimeProvider(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    public DnsResolver(TimeProvider timeProvider, ILogger<DnsResolver> logger) : this(OperatingSystem.IsLinux() || OperatingSystem.IsMacOS() ? ResolvConf.GetOptions() : NetworkInfo.GetOptions())
    {
        _timeProvider = timeProvider;
        _logger = logger;
    }

    internal DnsResolver(ResolverOptions options)
    {
        _logger = NullLogger<DnsResolver>.Instance;
        _options = options;
        Debug.Assert(_options.Servers.Count > 0);

        if (options.Timeout != Timeout.InfiniteTimeSpan)
        {
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(options.Timeout, TimeSpan.Zero);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(options.Timeout, s_maxTimeout);
        }
    }

    internal DnsResolver(IEnumerable<IPEndPoint> servers) : this(new ResolverOptions(servers.ToArray()))
    {
    }

    internal DnsResolver(IPEndPoint server) : this(new ResolverOptions(server))
    {
    }

    public ValueTask<ServiceResult[]> ResolveServiceAsync(string name, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        cancellationToken.ThrowIfCancellationRequested();

        byte[] buffer = ArrayPool<byte>.Shared.Rent(256);
        try
        {
            EncodedDomainName dnsSafeName = GetNormalizedHostName(name, buffer);
            return SendQueryWithTelemetry(name, dnsSafeName, QueryType.SRV, ProcessResponse, cancellationToken);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }

        static (SendQueryError, ServiceResult[]) ProcessResponse(EncodedDomainName dnsSafeName, QueryType queryType, DnsResponse response)
        {
            var results = new List<ServiceResult>(response.Answers.Count);

            foreach (var answer in response.Answers)
            {
                if (answer.Type == QueryType.SRV)
                {
                    if (!DnsPrimitives.TryReadService(answer.Data, out ushort priority, out ushort weight, out ushort port, out EncodedDomainName target, out int bytesRead) || bytesRead != answer.Data.Length)
                    {
                        return (SendQueryError.MalformedResponse, []);
                    }

                    List<AddressResult> addresses = new List<AddressResult>();
                    foreach (var additional in response.Additionals)
                    {
                        // From RFC 2782:
                        //
                        //     Target
                        //         The domain name of the target host.  There MUST be one or more
                        //         address records for this name, the name MUST NOT be an alias (in
                        //         the sense of RFC 1034 or RFC 2181).  Implementors are urged, but
                        //         not required, to return the address record(s) in the Additional
                        //         Data section.  Unless and until permitted by future standards
                        //         action, name compression is not to be used for this field.
                        //
                        //         A Target of "." means that the service is decidedly not
                        //         available at this domain.
                        if (additional.Name.Equals(target) && (additional.Type == QueryType.A || additional.Type == QueryType.AAAA))
                        {
                            addresses.Add(new AddressResult(response.CreatedAt.AddSeconds(additional.Ttl), new IPAddress(additional.Data.Span)));
                        }
                    }

                    results.Add(new ServiceResult(response.CreatedAt.AddSeconds(answer.Ttl), priority, weight, port, target.ToString(), addresses.ToArray()));
                }
            }

            return (SendQueryError.NoError, results.ToArray());
        }
    }

    public async ValueTask<AddressResult[]> ResolveIPAddressesAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.Equals(name, "localhost", StringComparison.OrdinalIgnoreCase))
        {
            // name localhost exists outside of DNS and can't be resolved by a DNS server
            int len = (Socket.OSSupportsIPv4 ? 1 : 0) + (Socket.OSSupportsIPv6 ? 1 : 0);
            AddressResult[] res = new AddressResult[len];

            int index = 0;
            if (Socket.OSSupportsIPv6) // prefer IPv6
            {
                res[index] = new AddressResult(DateTime.MaxValue, IPAddress.IPv6Loopback);
                index++;
            }
            if (Socket.OSSupportsIPv4)
            {
                res[index] = new AddressResult(DateTime.MaxValue, IPAddress.Loopback);
            }

            return res;
        }

        var ipv4AddressesTask = ResolveIPAddressesAsync(name, AddressFamily.InterNetwork, cancellationToken);
        var ipv6AddressesTask = ResolveIPAddressesAsync(name, AddressFamily.InterNetworkV6, cancellationToken);

        AddressResult[] ipv4Addresses = await ipv4AddressesTask.ConfigureAwait(false);
        AddressResult[] ipv6Addresses = await ipv6AddressesTask.ConfigureAwait(false);

        AddressResult[] results = new AddressResult[ipv4Addresses.Length + ipv6Addresses.Length];
        ipv6Addresses.CopyTo(results, 0);
        ipv4Addresses.CopyTo(results, ipv6Addresses.Length);
        return results;
    }

    public ValueTask<AddressResult[]> ResolveIPAddressesAsync(string name, AddressFamily addressFamily, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        cancellationToken.ThrowIfCancellationRequested();

        if (addressFamily != AddressFamily.InterNetwork && addressFamily != AddressFamily.InterNetworkV6)
        {
            throw new ArgumentOutOfRangeException(nameof(addressFamily), addressFamily, "Invalid address family");
        }

        if (string.Equals(name, "localhost", StringComparison.OrdinalIgnoreCase))
        {
            // name localhost exists outside of DNS and can't be resolved by a DNS server
            if (addressFamily == AddressFamily.InterNetwork && Socket.OSSupportsIPv4)
            {
                return ValueTask.FromResult<AddressResult[]>([new AddressResult(DateTime.MaxValue, IPAddress.Loopback)]);
            }
            else if (addressFamily == AddressFamily.InterNetworkV6 && Socket.OSSupportsIPv6)
            {
                return ValueTask.FromResult<AddressResult[]>([new AddressResult(DateTime.MaxValue, IPAddress.IPv6Loopback)]);
            }

            return ValueTask.FromResult<AddressResult[]>([]);
        }

        byte[] buffer = ArrayPool<byte>.Shared.Rent(256);
        try
        {
            EncodedDomainName dnsSafeName = GetNormalizedHostName(name, buffer);
            var queryType = addressFamily == AddressFamily.InterNetwork ? QueryType.A : QueryType.AAAA;
            return SendQueryWithTelemetry(name, dnsSafeName, queryType, ProcessResponse, cancellationToken);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }

        static (SendQueryError error, AddressResult[] result) ProcessResponse(EncodedDomainName dnsSafeName, QueryType queryType, DnsResponse response)
        {
            List<AddressResult> results = new List<AddressResult>(response.Answers.Count);

            // Servers send back CNAME records together with associated A/AAAA records. Servers
            // send only those CNAME records relevant to the query, and if there is a CNAME record,
            // there should not be other records associated with the name. Therefore, we simply follow
            // the list of CNAME aliases until we get to the primary name and return the A/AAAA records
            // associated.
            //
            // more info: https://datatracker.ietf.org/doc/html/rfc1034#section-3.6.2
            //
            // Most of the servers send the CNAME records in order so that we can sequentially scan the
            // answers, but nothing prevents the records from being in arbitrary order. Attempt the linear
            // scan first and fallback to a slower but more robust method if necessary.

            bool success = true;
            EncodedDomainName currentAlias = dnsSafeName;

            foreach (var answer in response.Answers)
            {
                switch (answer.Type)
                {
                    case QueryType.CNAME:
                        if (!TryReadTarget(answer, response.RawMessageBytes, out EncodedDomainName target))
                        {
                            return (SendQueryError.MalformedResponse, []);
                        }

                        if (answer.Name.Equals(currentAlias))
                        {
                            currentAlias = target;
                            continue;
                        }

                        break;

                    case var type when type == queryType:
                        if (!TryReadAddress(answer, queryType, out IPAddress? address))
                        {
                            return (SendQueryError.MalformedResponse, []);
                        }

                        if (answer.Name.Equals(currentAlias))
                        {
                            results.Add(new AddressResult(response.CreatedAt.AddSeconds(answer.Ttl), address));
                            continue;
                        }

                        break;
                }

                // unexpected name or record type, fall back to more robust path
                results.Clear();
                success = false;
                break;
            }

            if (success)
            {
                return (SendQueryError.NoError, results.ToArray());
            }

            // more expensive path for uncommon (but valid) cases where CNAME records are out of order. Use of Dictionary
            // allows us to stay within O(n) complexity for the number of answers, but we will use more memory.
            Dictionary<EncodedDomainName, EncodedDomainName> aliasMap = new();
            Dictionary<EncodedDomainName, List<AddressResult>> aRecordMap = new();
            foreach (var answer in response.Answers)
            {
                if (answer.Type == QueryType.CNAME)
                {
                    // map the alias to the target name
                    if (!TryReadTarget(answer, response.RawMessageBytes, out EncodedDomainName target))
                    {
                        return (SendQueryError.MalformedResponse, []);
                    }

                    if (!aliasMap.TryAdd(answer.Name, target))
                    {
                        // Duplicate CNAME record
                        return (SendQueryError.MalformedResponse, []);
                    }
                }

                if (answer.Type == queryType)
                {
                    if (!TryReadAddress(answer, queryType, out IPAddress? address))
                    {
                        return (SendQueryError.MalformedResponse, []);
                    }

                    if (!aRecordMap.TryGetValue(answer.Name, out List<AddressResult>? addressList))
                    {
                        addressList = new List<AddressResult>();
                        aRecordMap.Add(answer.Name, addressList);
                    }

                    addressList.Add(new AddressResult(response.CreatedAt.AddSeconds(answer.Ttl), address));
                }
            }

            // follow the CNAME chain, limit the maximum number of iterations to avoid infinite loops.
            int i = 0;
            currentAlias = dnsSafeName;
            while (aliasMap.TryGetValue(currentAlias, out EncodedDomainName nextAlias))
            {
                if (i >= aliasMap.Count)
                {
                    // circular CNAME chain
                    return (SendQueryError.MalformedResponse, []);
                }

                i++;

                if (aRecordMap.ContainsKey(currentAlias))
                {
                    // both CNAME record and A/AAAA records exist for the current alias
                    return (SendQueryError.MalformedResponse, []);
                }

                currentAlias = nextAlias;
            }

            // Now we have the final target name, check if we have any A/AAAA records for it.
            aRecordMap.TryGetValue(currentAlias, out List<AddressResult>? finalAddressList);
            return (SendQueryError.NoError, finalAddressList?.ToArray() ?? []);

            static bool TryReadTarget(in DnsResourceRecord record, ArraySegment<byte> messageBytes, out EncodedDomainName target)
            {
                Debug.Assert(record.Type == QueryType.CNAME, "Only CNAME records should be processed here.");

                target = default;

                // some servers use domain name compression even inside CNAME records. In order to decode those
                // correctly, we need to pass the entire message to TryReadQName. The Data span inside the record
                // should be backed by the array containing the entire DNS message. We just need to account for the
                // 2 byte offset in case of TCP fallback.
                var gotArray = MemoryMarshal.TryGetArray(record.Data, out ArraySegment<byte> segment);
                Debug.Assert(gotArray, "Failed to get array segment");
                Debug.Assert(segment.Array == messageBytes.Array, "record data backed by different array than the original message");

                int messageOffset = messageBytes.Offset;

                bool result = DnsPrimitives.TryReadQName(segment.Array.AsMemory(messageOffset, segment.Offset + segment.Count - messageOffset), segment.Offset, out EncodedDomainName targetName, out int bytesRead) && bytesRead == record.Data.Length;
                if (result)
                {
                    target = targetName;
                }

                return result;
            }

            static bool TryReadAddress(in DnsResourceRecord record, QueryType type, [NotNullWhen(true)] out IPAddress? target)
            {
                Debug.Assert(record.Type is QueryType.A or QueryType.AAAA, "Only CNAME records should be processed here.");

                target = null;
                if (record.Type == QueryType.A && record.Data.Length != IPv4Length ||
                    record.Type == QueryType.AAAA && record.Data.Length != IPv6Length)
                {
                    return false;
                }

                target = new IPAddress(record.Data.Span);
                return true;
            }
        }
    }

    private async ValueTask<TResult[]> SendQueryWithTelemetry<TResult>(string name, EncodedDomainName dnsSafeName, QueryType queryType, Func<EncodedDomainName, QueryType, DnsResponse, (SendQueryError error, TResult[] result)> processResponseFunc, CancellationToken cancellationToken)
    {
        NameResolutionActivity activity = Telemetry.StartNameResolution(name, queryType, _timeProvider.GetTimestamp());
        (SendQueryError error, TResult[] result) = await SendQueryWithRetriesAsync(name, dnsSafeName, queryType, processResponseFunc, cancellationToken).ConfigureAwait(false);
        Telemetry.StopNameResolution(name, queryType, activity, null, error, _timeProvider.GetTimestamp());

        return result;
    }

    internal struct SendQueryResult
    {
        public DnsResponse Response;
        public SendQueryError Error;
    }

    async ValueTask<(SendQueryError error, TResult[] result)> SendQueryWithRetriesAsync<TResult>(string name, EncodedDomainName dnsSafeName, QueryType queryType, Func<EncodedDomainName, QueryType, DnsResponse, (SendQueryError error, TResult[] result)> processResponseFunc, CancellationToken cancellationToken)
    {
        SendQueryError lastError = SendQueryError.InternalError; // will be overwritten by the first attempt
        for (int index = 0; index < _options.Servers.Count; index++)
        {
            IPEndPoint serverEndPoint = _options.Servers[index];

            for (int attempt = 1; attempt <= _options.Attempts; attempt++)
            {
                DnsResponse response = default;
                try
                {
                    TResult[] results = Array.Empty<TResult>();

                    try
                    {
                        SendQueryResult queryResult = await SendQueryToServerWithTimeoutAsync(serverEndPoint, name, dnsSafeName, queryType, attempt, cancellationToken).ConfigureAwait(false);
                        lastError = queryResult.Error;
                        response = queryResult.Response;

                        if (lastError == SendQueryError.NoError)
                        {
                            // Given that result.Error is NoError, there should be at least one answer.
                            Debug.Assert(response.Answers.Count > 0);
                            (lastError, results) = processResponseFunc(dnsSafeName, queryType, queryResult.Response);
                        }
                    }
                    catch (SocketException ex)
                    {
                        Log.NetworkError(_logger, queryType, name, serverEndPoint, attempt, ex);
                        lastError = SendQueryError.NetworkError;
                    }
                    catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
                    {
                        Log.QueryError(_logger, queryType, name, serverEndPoint, attempt, ex);
                        lastError = SendQueryError.InternalError;
                    }

                    switch (lastError)
                    {
                        //
                        // Definitive answers, no point retrying
                        //
                        case SendQueryError.NoError:
                            return (lastError, results);

                        case SendQueryError.NameError:
                            // authoritative answer that the name does not exist, no point in retrying
                            Log.NameError(_logger, queryType, name, serverEndPoint, attempt);
                            return (lastError, results);

                        case SendQueryError.NoData:
                            // no data available for the name from authoritative server
                            Log.NoData(_logger, queryType, name, serverEndPoint, attempt);
                            return (lastError, results);

                        //
                        // Transient errors, retry on the same server
                        //
                        case SendQueryError.Timeout:
                            Log.Timeout(_logger, queryType, name, serverEndPoint, attempt);
                            continue;

                        case SendQueryError.NetworkError:
                            // TODO: retry with exponential backoff?
                            continue;

                        case SendQueryError.ServerError when response.Header.ResponseCode == QueryResponseCode.ServerFailure:
                            // ServerFailure may indicate transient failure with upstream DNS servers, retry on the same server
                            Log.ErrorResponseCode(_logger, queryType, name, serverEndPoint, response.Header.ResponseCode);
                            continue;

                        //
                        // Persistent errors, skip to the next server
                        //
                        case SendQueryError.ServerError:
                            // this should cover all response codes except NoError, NameError which are definite and handled above, and
                            // ServerFailure which is a transient error and handled above.
                            Log.ErrorResponseCode(_logger, queryType, name, serverEndPoint, response.Header.ResponseCode);
                            break;

                        case SendQueryError.MalformedResponse:
                            Log.MalformedResponse(_logger, queryType, name, serverEndPoint, attempt);
                            break;

                        case SendQueryError.InternalError:
                            // exception logged above.
                            break;
                    }

                    // actual break that causes skipping to the next server
                    break;
                }
                finally
                {
                    response.Dispose();
                }
            }
        }

        // if we get here, we exhausted all servers and all attempts
        return (lastError, []);
    }

    internal async ValueTask<SendQueryResult> SendQueryToServerWithTimeoutAsync(IPEndPoint serverEndPoint, string name, EncodedDomainName dnsSafeName, QueryType queryType, int attempt, CancellationToken cancellationToken)
    {
        (CancellationTokenSource cts, bool disposeTokenSource, CancellationTokenSource pendingRequestsCts) = PrepareCancellationTokenSource(cancellationToken);

        try
        {
            return await SendQueryToServerAsync(serverEndPoint, name, dnsSafeName, queryType, attempt, cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (
            !cancellationToken.IsCancellationRequested && // not cancelled by the caller
            !pendingRequestsCts.IsCancellationRequested) // not cancelled by the global token (dispose)
                                                         // the only remaining token that could cancel this is the linked cts from the timeout.
        {
            Debug.Assert(cts.Token.IsCancellationRequested);
            return new SendQueryResult { Error = SendQueryError.Timeout };
        }
        catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested && ex.CancellationToken != cancellationToken)
        {
            // cancellation was initiated by the caller, but exception was triggered by a linked token,
            // rethrow the exception with the caller's token.
            cancellationToken.ThrowIfCancellationRequested();
            throw new UnreachableException();
        }
        finally
        {
            if (disposeTokenSource)
            {
                cts.Dispose();
            }
        }
    }

    private async ValueTask<SendQueryResult> SendQueryToServerAsync(IPEndPoint serverEndPoint, string name, EncodedDomainName dnsSafeName, QueryType queryType, int attempt, CancellationToken cancellationToken)
    {
        Log.Query(_logger, queryType, name, serverEndPoint, attempt);

        SendQueryError sendError = SendQueryError.NoError;
        DateTime queryStartedTime = _timeProvider.GetUtcNow().DateTime;
        (DnsDataReader responseReader, DnsMessageHeader header) = await SendDnsQueryCoreUdpAsync(serverEndPoint, dnsSafeName, queryType, cancellationToken).ConfigureAwait(false);

        try
        {
            if (header.IsResultTruncated)
            {
                Log.ResultTruncated(_logger, queryType, name, serverEndPoint, 0);
                responseReader.Dispose();
                // TCP fallback
                (responseReader, header, sendError) = await SendDnsQueryCoreTcpAsync(serverEndPoint, dnsSafeName, queryType, cancellationToken).ConfigureAwait(false);
            }

            if (sendError != SendQueryError.NoError)
            {
                // we failed to get back any response
                return new SendQueryResult { Error = sendError };
            }

            if ((uint)header.ResponseCode > (uint)QueryResponseCode.Refused)
            {
                // Response code is outside of valid range
                return new SendQueryResult
                {
                    Response = new DnsResponse(ArraySegment<byte>.Empty, header, queryStartedTime, queryStartedTime, null!, null!, null!),
                    Error = SendQueryError.MalformedResponse
                };
            }

            // Recheck that the server echoes back the DNS question
            if (header.QueryCount != 1 ||
                !responseReader.TryReadQuestion(out var qName, out var qType, out var qClass) ||
                !dnsSafeName.Equals(qName) || qType != queryType || qClass != QueryClass.Internet)
            {
                // DNS Question mismatch
                return new SendQueryResult
                {
                    Response = new DnsResponse(ArraySegment<byte>.Empty, header, queryStartedTime, queryStartedTime, null!, null!, null!),
                    Error = SendQueryError.MalformedResponse
                };
            }

            // Structurally separate the resource records, this will validate only the
            // "outside structure" of the resource record, it will not validate the content.
            int ttl = int.MaxValue;
            if (!TryReadRecords(header.AnswerCount, ref ttl, ref responseReader, out List<DnsResourceRecord>? answers) ||
                !TryReadRecords(header.AuthorityCount, ref ttl, ref responseReader, out List<DnsResourceRecord>? authorities) ||
                !TryReadRecords(header.AdditionalRecordCount, ref ttl, ref responseReader, out List<DnsResourceRecord>? additionals))
            {
                return new SendQueryResult
                {
                    Response = new DnsResponse(ArraySegment<byte>.Empty, header, queryStartedTime, queryStartedTime, null!, null!, null!),
                    Error = SendQueryError.MalformedResponse
                };
            }

            DateTime expirationTime =
                (answers.Count + authorities.Count + additionals.Count) > 0 ? queryStartedTime.AddSeconds(ttl) : queryStartedTime;

            SendQueryError validationError = ValidateResponse(header.ResponseCode, queryStartedTime, answers, authorities, ref expirationTime);

            // we transfer ownership of RawData to the response
            DnsResponse response = new DnsResponse(responseReader.MessageBuffer, header, queryStartedTime, expirationTime, answers, authorities, additionals);
            responseReader = default; // avoid disposing (and returning RawData to the pool)

            return new SendQueryResult { Response = response, Error = validationError };
        }
        finally
        {
            responseReader.Dispose();
        }

        static bool TryReadRecords(int count, ref int ttl, ref DnsDataReader reader, out List<DnsResourceRecord> records)
        {
            // Since `count` is attacker controlled, limit the initial capacity
            // to 32 items to avoid excessive memory allocation. More than 32
            // records are unusual so we don't need to optimize for them.
            records = new(Math.Min(count, 32));

            for (int i = 0; i < count; i++)
            {
                if (!reader.TryReadResourceRecord(out var record))
                {
                    return false;
                }

                ttl = Math.Min(ttl, record.Ttl);
                records.Add(new DnsResourceRecord(record.Name, record.Type, record.Class, record.Ttl, record.Data));
            }

            return true;
        }
    }

    internal static bool GetNegativeCacheExpiration(DateTime createdAt, List<DnsResourceRecord> authorities, out DateTime expiration)
    {
        //
        // RFC 2308 Section 5 - Caching Negative Answers
        //
        //    Like normal answers negative answers have a time to live (TTL).  As
        //    there is no record in the answer section to which this TTL can be
        //    applied, the TTL must be carried by another method.  This is done by
        //    including the SOA record from the zone in the authority section of
        //    the reply.  When the authoritative server creates this record its TTL
        //    is taken from the minimum of the SOA.MINIMUM field and SOA's TTL.
        //    This TTL decrements in a similar manner to a normal cached answer and
        //    upon reaching zero (0) indicates the cached negative answer MUST NOT
        //    be used again.
        //

        DnsResourceRecord? soa = authorities.FirstOrDefault(r => r.Type == QueryType.SOA);
        if (soa != null && DnsPrimitives.TryReadSoa(soa.Value.Data, out _, out _, out _, out _, out _, out _, out uint minimum, out _))
        {
            expiration = createdAt.AddSeconds(Math.Min(minimum, soa.Value.Ttl));
            return true;
        }

        expiration = default;
        return false;
    }

    internal static SendQueryError ValidateResponse(QueryResponseCode responseCode, DateTime createdAt, List<DnsResourceRecord> answers, List<DnsResourceRecord> authorities, ref DateTime expiration)
    {
        if (responseCode == QueryResponseCode.NoError)
        {
            if (answers.Count > 0)
            {
                return SendQueryError.NoError;
            }
            //
            // RFC 2308 Section 2.2 - No Data
            //
            //    NODATA is indicated by an answer with the RCODE set to NOERROR and no
            //    relevant answers in the answer section.  The authority section will
            //    contain an SOA record, or there will be no NS records there.
            //
            //
            // RFC 2308 Section 5 - Caching Negative Answers
            //
            //    A negative answer that resulted from a no data error (NODATA) should
            //    be cached such that it can be retrieved and returned in response to
            //    another query for the same <QNAME, QTYPE, QCLASS> that resulted in
            //    the cached negative response.
            //
            if (!authorities.Any(r => r.Type == QueryType.NS) && GetNegativeCacheExpiration(createdAt, authorities, out DateTime newExpiration))
            {
                expiration = newExpiration;
                // _cache.TryAdd(name, queryType, expiration, Array.Empty<T>());
            }
            return SendQueryError.NoData;
        }

        if (responseCode == QueryResponseCode.NameError)
        {
            //
            // RFC 2308 Section 5 - Caching Negative Answers
            //
            //    A negative answer that resulted from a name error (NXDOMAIN) should
            //    be cached such that it can be retrieved and returned in response to
            //    another query for the same <QNAME, QCLASS> that resulted in the
            //    cached negative response.
            //
            if (GetNegativeCacheExpiration(createdAt, authorities, out DateTime newExpiration))
            {
                expiration = newExpiration;
                // _cache.TryAddNonexistent(name, expiration);
            }

            return SendQueryError.NameError;
        }

        return SendQueryError.ServerError;
    }

    internal static async ValueTask<(DnsDataReader reader, DnsMessageHeader header)> SendDnsQueryCoreUdpAsync(IPEndPoint serverEndPoint, EncodedDomainName dnsSafeName, QueryType queryType, CancellationToken cancellationToken)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(512);
        try
        {
            Memory<byte> memory = buffer;
            (ushort transactionId, int length) = EncodeQuestion(memory, dnsSafeName, queryType);

            using var socket = new Socket(serverEndPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            await socket.SendToAsync(memory.Slice(0, length), SocketFlags.None, serverEndPoint, cancellationToken).ConfigureAwait(false);

            DnsDataReader responseReader;
            DnsMessageHeader header;

            while (true)
            {
                // Because this is UDP, the response must be in a single packet,
                // if the response does not fit into a single UDP packet, the server will
                // set the Truncated flag in the header, and we will need to retry with TCP.
                int packetLength = await socket.ReceiveAsync(memory, SocketFlags.None, cancellationToken).ConfigureAwait(false);

                if (packetLength < DnsMessageHeader.HeaderLength)
                {
                    continue;
                }

                responseReader = new DnsDataReader(new ArraySegment<byte>(buffer, 0, packetLength), true);
                if (!responseReader.TryReadHeader(out header) ||
                    header.TransactionId != transactionId ||
                    !header.IsResponse)
                {
                    // header mismatch, this is not a response to our query
                    continue;
                }

                // ownership of the buffer is transferred to the reader, caller will dispose.
                buffer = null!;
                return (responseReader, header);
            }
        }
        finally
        {
            if (buffer != null)
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
    }

    internal static async ValueTask<(DnsDataReader reader, DnsMessageHeader header, SendQueryError error)> SendDnsQueryCoreTcpAsync(IPEndPoint serverEndPoint, EncodedDomainName dnsSafeName, QueryType queryType, CancellationToken cancellationToken)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(8 * 1024);
        try
        {
            // When sending over TCP, the message is prefixed by 2B length
            (ushort transactionId, int length) = EncodeQuestion(buffer.AsMemory(2), dnsSafeName, queryType);
            BinaryPrimitives.WriteUInt16BigEndian(buffer, (ushort)length);

            using var socket = new Socket(serverEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            await socket.ConnectAsync(serverEndPoint, cancellationToken).ConfigureAwait(false);
            await socket.SendAsync(buffer.AsMemory(0, length + 2), SocketFlags.None, cancellationToken).ConfigureAwait(false);

            int responseLength = -1;
            int bytesRead = 0;
            while (responseLength < 0 || bytesRead < length + 2)
            {
                int read = await socket.ReceiveAsync(buffer.AsMemory(bytesRead), SocketFlags.None, cancellationToken).ConfigureAwait(false);
                bytesRead += read;

                if (read == 0)
                {
                    // connection closed before receiving complete response message
                    return (default, default, SendQueryError.MalformedResponse);
                }

                if (responseLength < 0 && bytesRead >= 2)
                {
                    responseLength = BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(0, 2));

                    if (responseLength > buffer.Length)
                    {
                        // even though this is user-controlled pre-allocation, it is limited to
                        // 64 kB, so it should be fine.
                        var largerBuffer = ArrayPool<byte>.Shared.Rent(responseLength);
                        Array.Copy(buffer, largerBuffer, bytesRead);
                        ArrayPool<byte>.Shared.Return(buffer);
                        buffer = largerBuffer;
                    }
                }
            }

            DnsDataReader responseReader = new DnsDataReader(new ArraySegment<byte>(buffer, 2, responseLength), true);
            if (!responseReader.TryReadHeader(out DnsMessageHeader header) ||
                header.TransactionId != transactionId ||
                !header.IsResponse)
            {
                // header mismatch on TCP fallback
                return (default, default, SendQueryError.MalformedResponse);
            }

            // transfer ownership of buffer to the caller
            buffer = null!;
            return (responseReader, header, SendQueryError.NoError);
        }
        finally
        {
            if (buffer != null)
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
    }

    private static (ushort id, int length) EncodeQuestion(Memory<byte> buffer, EncodedDomainName dnsSafeName, QueryType queryType)
    {
        DnsMessageHeader header = new DnsMessageHeader
        {
            TransactionId = (ushort)RandomNumberGenerator.GetInt32(ushort.MaxValue + 1),
            QueryFlags = QueryFlags.RecursionDesired,
            QueryCount = 1
        };

        DnsDataWriter writer = new DnsDataWriter(buffer);
        if (!writer.TryWriteHeader(header) ||
            !writer.TryWriteQuestion(dnsSafeName, queryType, QueryClass.Internet))
        {
            // should never happen since we validated the name length before
            throw new InvalidOperationException("Buffer too small");
        }
        return (header.TransactionId, writer.Position);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;

            // Cancel all pending requests (if any). Note that we don't call CancelPendingRequests() but cancel
            // the CTS directly. The reason is that CancelPendingRequests() would cancel the current CTS and create
            // a new CTS. We don't want a new CTS in this case.
            _pendingRequestsCts.Cancel();
            _pendingRequestsCts.Dispose();
        }
    }

    private (CancellationTokenSource TokenSource, bool DisposeTokenSource, CancellationTokenSource PendingRequestsCts) PrepareCancellationTokenSource(CancellationToken cancellationToken)
    {
        // We need a CancellationTokenSource to use with the request.  We always have the global
        // _pendingRequestsCts to use, plus we may have a token provided by the caller, and we may
        // have a timeout.  If we have a timeout or a caller-provided token, we need to create a new
        // CTS (we can't, for example, timeout the pending requests CTS, as that could cancel other
        // unrelated operations).  Otherwise, we can use the pending requests CTS directly.

        // Snapshot the current pending requests cancellation source. It can change concurrently due to cancellation being requested
        // and it being replaced, and we need a stable view of it: if cancellation occurs and the caller's token hasn't been canceled,
        // it's either due to this source or due to the timeout, and checking whether this source is the culprit is reliable whereas
        // it's more approximate checking elapsed time.
        CancellationTokenSource pendingRequestsCts = _pendingRequestsCts;
        TimeSpan timeout = _options.Timeout;

        bool hasTimeout = timeout != System.Threading.Timeout.InfiniteTimeSpan;
        if (hasTimeout || cancellationToken.CanBeCanceled)
        {
            CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, pendingRequestsCts.Token);
            if (hasTimeout)
            {
                cts.CancelAfter(timeout);
            }

            return (cts, DisposeTokenSource: true, pendingRequestsCts);
        }

        return (pendingRequestsCts, DisposeTokenSource: false, pendingRequestsCts);
    }

    private static EncodedDomainName GetNormalizedHostName(string name, Memory<byte> buffer)
    {
        if (!DnsPrimitives.TryWriteQName(buffer.Span, name, out _))
        {
            throw new ArgumentException($"'{name}' is not a valid DNS name.", nameof(name));
        }

        List<ReadOnlyMemory<byte>> labels = new();
        while (true)
        {
            int len = buffer.Span[0];

            if (len == 0)
            {
                // root label, we are finished
                break;
            }

            labels.Add(buffer.Slice(1, len));
            buffer = buffer.Slice(len + 1);
        }

        return new EncodedDomainName(labels);
    }
}
