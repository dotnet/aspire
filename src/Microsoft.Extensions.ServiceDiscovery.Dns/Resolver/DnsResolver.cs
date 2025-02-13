// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Globalization;
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

    public async ValueTask<ServiceResult[]> ResolveServiceAsync(string name, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        cancellationToken.ThrowIfCancellationRequested();

        name = GetNormalizedHostName(name);

        NameResolutionActivity activity = Telemetry.StartNameResolution(name, QueryType.SRV, _timeProvider.GetTimestamp());
        SendQueryResult result = await SendQueryWithRetriesAsync(name, QueryType.SRV, cancellationToken).ConfigureAwait(false);

        if (result.Error is not SendQueryError.NoError)
        {
            Telemetry.StopNameResolution(name, QueryType.SRV, activity, null, result.Error, _timeProvider.GetTimestamp());
            return Array.Empty<ServiceResult>();
        }

        using DnsResponse response = result.Response;

        var results = new List<ServiceResult>(response.Answers.Count);

        foreach (var answer in response.Answers)
        {
            if (answer.Type == QueryType.SRV)
            {
                bool success = DnsPrimitives.TryReadService(answer.Data.Span, out ushort priority, out ushort weight, out ushort port, out string? target, out _);
                Debug.Assert(success, "Failed to read SRV");

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
                    if (additional.Name == target && (additional.Type == QueryType.A || additional.Type == QueryType.AAAA))
                    {
                        addresses.Add(new AddressResult(response.CreatedAt.AddSeconds(additional.Ttl), new IPAddress(additional.Data.Span)));
                    }
                }

                results.Add(new ServiceResult(response.CreatedAt.AddSeconds(answer.Ttl), priority, weight, port, target!, addresses.ToArray()));
            }
        }

        ServiceResult[] res = results.ToArray();
        Telemetry.StopNameResolution(name, QueryType.SRV, activity, res, result.Error, _timeProvider.GetTimestamp());
        return res;
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

    public async ValueTask<AddressResult[]> ResolveIPAddressesAsync(string name, AddressFamily addressFamily, CancellationToken cancellationToken = default)
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
                return [new AddressResult(DateTime.MaxValue, IPAddress.Loopback)];
            }
            else if (addressFamily == AddressFamily.InterNetworkV6 && Socket.OSSupportsIPv6)
            {
                return [new AddressResult(DateTime.MaxValue, IPAddress.IPv6Loopback)];
            }

            return Array.Empty<AddressResult>();
        }

        name = GetNormalizedHostName(name);

        var queryType = addressFamily == AddressFamily.InterNetwork ? QueryType.A : QueryType.AAAA;
        NameResolutionActivity activity = Telemetry.StartNameResolution(name, queryType, _timeProvider.GetTimestamp());
        SendQueryResult result = await SendQueryWithRetriesAsync(name, queryType, cancellationToken).ConfigureAwait(false);
        if (result.Error is not SendQueryError.NoError)
        {
            Telemetry.StopNameResolution(name, queryType, activity, null, result.Error, _timeProvider.GetTimestamp());
            return Array.Empty<AddressResult>();
        }

        using DnsResponse response = result.Response;

        // Given that result.Error is NoError, there should be at least one answer.
        Debug.Assert(response.Answers.Count > 0);
        var results = new List<AddressResult>(response.Answers.Count);

        // Servers send back CNAME records together with associated A/AAAA records. Servers
        // send only those CNAME records relevant to the query, and if there is a CNAME record,
        // there should not be other records associated with the name. Therefore, we simply follow
        // the list of CNAME aliases until we get to the primary name and return the A/AAAA records
        // associated.
        //
        // more info: https://datatracker.ietf.org/doc/html/rfc1034#section-3.6.2
        //
        // Most of the servers send the CNAME records in order so that we can sequentially scan the
        // answers, but nothing prevents the records from being in arbitrary order. Therefore, when
        // we encounter a CNAME record, we continue down the list and allow looping back to the beginning
        // in case the CNAME chain is not in order.
        //
        string currentAlias = name;
        int i = 0;
        int endIndex = 0;

        do
        {
            DnsResourceRecord answer = response.Answers[i];

            if (answer.Name == currentAlias)
            {
                if (answer.Type == QueryType.CNAME)
                {
                    // Although RFC does not necessarily allow pointer segments in CNAME domain names, some servers do use them
                    // so we need to pass the entire buffer to TryReadQName with the proper offset. The data should be always
                    // backed by the array containing the full response.

                    var success = MemoryMarshal.TryGetArray(answer.Data, out ArraySegment<byte> segment);
                    Debug.Assert(success, "Failed to get array segment");
                    if (!DnsPrimitives.TryReadQName(segment.Array.AsSpan(0, segment.Offset + segment.Count), segment.Offset, out currentAlias!, out _))
                    {
                        // TODO: how to handle corrupted responses?
                        throw new InvalidOperationException("Failed to parse CNAME record");
                    }

                    // We need to start over. start with following answers and allow looping back
                    endIndex = i;

                    if (string.Equals(currentAlias, name, StringComparison.OrdinalIgnoreCase))
                    {
                        // CNAME records looped back to original question dns name (=> malformed response). Stop processing.
                        break;
                    }
                }
                else if (answer.Type == queryType)
                {
                    Debug.Assert(answer.Data.Length == IPv4Length || answer.Data.Length == IPv6Length);
                    results.Add(new AddressResult(response.CreatedAt.AddSeconds(answer.Ttl), new IPAddress(answer.Data.Span)));
                }
            }

            i = (i + 1) % response.Answers.Count;
        }
        while (i != endIndex);

        AddressResult[] res = results.ToArray();
        Telemetry.StopNameResolution(name, queryType, activity, res, result.Error, _timeProvider.GetTimestamp());
        return res;
    }

    internal struct SendQueryResult
    {
        public DnsResponse Response;
        public SendQueryError Error;
    }

    async ValueTask<SendQueryResult> SendQueryWithRetriesAsync(string name, QueryType queryType, CancellationToken cancellationToken)
    {
        SendQueryResult? result = default;

        for (int index = 0; index < _options.Servers.Count; index++)
        {
            IPEndPoint serverEndPoint = _options.Servers[index];

            for (int attempt = 1; attempt <= _options.Attempts; attempt++)
            {
                try
                {
                    SendQueryResult newResult = await SendQueryToServerWithTimeoutAsync(serverEndPoint, name, queryType, attempt, cancellationToken).ConfigureAwait(false);

                    if (result.HasValue)
                    {
                        result.Value.Response.Dispose();
                    }

                    result = newResult;
                }
                catch (SocketException ex)
                {
                    Log.NetworkError(_logger, queryType, name, serverEndPoint, attempt, ex);
                    result = new SendQueryResult { Error = SendQueryError.NetworkError };
                }
                catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
                {
                    Log.QueryError(_logger, queryType, name, serverEndPoint, attempt, ex);
                    result = new SendQueryResult { Error = SendQueryError.InternalError };
                }

                switch (result.Value.Error)
                {
                    //
                    // Definitive answers, no point retrying
                    //
                    case SendQueryError.NoError:
                        return result.Value;

                    case SendQueryError.NameError:
                        // authoritative answer that the name does not exist, no point in retrying
                        Log.NameError(_logger, queryType, name, serverEndPoint, attempt);
                        return result.Value;

                    case SendQueryError.NoData:
                        // no data available for the name from authoritative server
                        Log.NoData(_logger, queryType, name, serverEndPoint, attempt);
                        return result.Value;

                    //
                    // Transient errors, retry on the same server
                    //
                    case SendQueryError.Timeout:
                        Log.Timeout(_logger, queryType, name, serverEndPoint, attempt);
                        continue;

                    case SendQueryError.NetworkError:
                        // TODO: retry with exponential backoff?
                        continue;

                    case SendQueryError.ServerError when result.Value.Response!.Header.ResponseCode == QueryResponseCode.ServerFailure:
                        // ServerFailure may indicate transient failure with upstream DNS servers, retry on the same server
                        Log.ErrorResponseCode(_logger, queryType, name, serverEndPoint, result.Value.Response.Header.ResponseCode);
                        continue;

                    //
                    // Persistent errors, skip to the next server
                    //
                    case SendQueryError.ServerError:
                        // this should cover all response codes except NoError, NameError which are definite and handled above, and
                        // ServerFailure which is a transient error and handled above.
                        Log.ErrorResponseCode(_logger, queryType, name, serverEndPoint, result.Value.Response.Header.ResponseCode);
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
        }

        // we should have a result by now
        Debug.Assert(result.HasValue);

        if (!result.HasValue)
        {
            result = new SendQueryResult { Error = SendQueryError.InternalError };
        }

        return result!.Value;
    }

    internal async ValueTask<SendQueryResult> SendQueryToServerWithTimeoutAsync(IPEndPoint serverEndPoint, string name, QueryType queryType, int attempt, CancellationToken cancellationToken)
    {
        (CancellationTokenSource cts, bool disposeTokenSource, CancellationTokenSource pendingRequestsCts) = PrepareCancellationTokenSource(cancellationToken);

        try
        {
            return await SendQueryToServerAsync(serverEndPoint, name, queryType, attempt, cts.Token).ConfigureAwait(false);
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

    private async ValueTask<SendQueryResult> SendQueryToServerAsync(IPEndPoint serverEndPoint, string name, QueryType queryType, int attempt, CancellationToken cancellationToken)
    {
        Log.Query(_logger, queryType, name, serverEndPoint, attempt);

        SendQueryError sendError = SendQueryError.NoError;
        DateTime queryStartedTime = _timeProvider.GetUtcNow().DateTime;
        (DnsDataReader responseReader, DnsMessageHeader header) = await SendDnsQueryCoreUdpAsync(serverEndPoint, name, queryType, cancellationToken).ConfigureAwait(false);

        try
        {
            if (header.IsResultTruncated)
            {
                Log.ResultTruncated(_logger, queryType, name, serverEndPoint, 0);
                responseReader.Dispose();
                // TCP fallback
                (responseReader, header, sendError) = await SendDnsQueryCoreTcpAsync(serverEndPoint, name, queryType, cancellationToken).ConfigureAwait(false);
            }

            if (sendError != SendQueryError.NoError)
            {
                // we failed to get back any response
                return new SendQueryResult { Error = sendError };
            }

            if (header.QueryCount != 1 ||
                !responseReader.TryReadQuestion(out var qName, out var qType, out var qClass) ||
                qName != name || qType != queryType || qClass != QueryClass.Internet)
            {
                // DNS Question mismatch
                return new SendQueryResult
                {
                    Response = new DnsResponse(Array.Empty<byte>(), header, queryStartedTime, queryStartedTime, null!, null!, null!),
                    Error = SendQueryError.MalformedResponse
                };
            }

            int ttl = int.MaxValue;
            List<DnsResourceRecord> answers = ReadRecords(header.AnswerCount, ref ttl, ref responseReader);
            List<DnsResourceRecord> authorities = ReadRecords(header.AuthorityCount, ref ttl, ref responseReader);
            List<DnsResourceRecord> additionals = ReadRecords(header.AdditionalRecordCount, ref ttl, ref responseReader);

            DateTime expirationTime =
                (answers.Count + authorities.Count + additionals.Count) > 0 ? queryStartedTime.AddSeconds(ttl) : queryStartedTime;

            SendQueryError validationError = ValidateResponse(header.ResponseCode, queryStartedTime, answers, authorities, ref expirationTime);

            // we transfer ownership of RawData to the response
            DnsResponse response = new(responseReader.RawData!, header, queryStartedTime, expirationTime, answers, authorities, additionals);
            responseReader = default; // avoid disposing (and returning RawData to the pool)

            return new SendQueryResult { Response = response, Error = validationError };
        }
        finally
        {
            responseReader.Dispose();
        }

        static List<DnsResourceRecord> ReadRecords(int count, ref int ttl, ref DnsDataReader reader)
        {
            List<DnsResourceRecord> records = new(count);

            for (int i = 0; i < count; i++)
            {
                if (!reader.TryReadResourceRecord(out var record))
                {
                    // TODO how to handle corrupted responses?
                    throw new InvalidOperationException("Invalid response: corrupted record");
                }

                ttl = Math.Min(ttl, record.Ttl);
                records.Add(new DnsResourceRecord(record.Name, record.Type, record.Class, record.Ttl, record.Data));
            }

            return records;
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
        if (soa != null && DnsPrimitives.TryReadSoa(soa.Value.Data.Span, out string? mname, out string? rname, out uint serial, out uint refresh, out uint retry, out uint expire, out uint minimum, out _))
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

    internal static async ValueTask<(DnsDataReader reader, DnsMessageHeader header)> SendDnsQueryCoreUdpAsync(IPEndPoint serverEndPoint, string name, QueryType queryType, CancellationToken cancellationToken)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(512);
        try
        {
            Memory<byte> memory = buffer;
            (ushort transactionId, int length) = EncodeQuestion(memory, name, queryType);

            using var socket = new Socket(serverEndPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            await socket.SendToAsync(memory.Slice(0, length), SocketFlags.None, serverEndPoint, cancellationToken).ConfigureAwait(false);

            DnsDataReader responseReader;
            DnsMessageHeader header;

            while (true)
            {
                int readLength = await socket.ReceiveAsync(memory, SocketFlags.None, cancellationToken).ConfigureAwait(false);

                if (readLength < DnsMessageHeader.HeaderLength)
                {
                    continue;
                }

                responseReader = new DnsDataReader(memory.Slice(0, readLength), buffer);
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

    internal static async ValueTask<(DnsDataReader reader, DnsMessageHeader header, SendQueryError error)> SendDnsQueryCoreTcpAsync(IPEndPoint serverEndPoint, string name, QueryType queryType, CancellationToken cancellationToken)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(8 * 1024);
        try
        {
            // When sending over TCP, the message is prefixed by 2B length
            (ushort transactionId, int length) = EncodeQuestion(buffer.AsMemory(2), name, queryType);
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

            DnsDataReader responseReader = new DnsDataReader(buffer.AsMemory(2, responseLength), buffer);
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

    private static (ushort id, int length) EncodeQuestion(Memory<byte> buffer, string name, QueryType queryType)
    {
        DnsMessageHeader header = new DnsMessageHeader
        {
            TransactionId = (ushort)RandomNumberGenerator.GetInt32(ushort.MaxValue + 1),
            QueryFlags = QueryFlags.RecursionDesired,
            QueryCount = 1
        };

        DnsDataWriter writer = new DnsDataWriter(buffer);
        if (!writer.TryWriteHeader(header) ||
            !writer.TryWriteQuestion(name, queryType, QueryClass.Internet))
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

    private static readonly IdnMapping s_idnMapping = new IdnMapping();

    private static string GetNormalizedHostName(string name)
    {
        // TODO: better exception message
        return s_idnMapping.GetAscii(name);
    }
}
