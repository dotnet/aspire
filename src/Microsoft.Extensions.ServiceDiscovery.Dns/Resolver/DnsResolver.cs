// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.Extensions.ServiceDiscovery.Dns.Resolver;

internal partial class DnsResolver : IDnsResolver, IDisposable
{
    private const int MaximumNameLength = 253;
    private const int IPv4Length = 4;
    private const int IPv6Length = 16;

    private static readonly TimeSpan s_maxTimeout = TimeSpan.FromMilliseconds(int.MaxValue);

    bool _disposed;
    private readonly ResolverOptions _options;
    private readonly CancellationTokenSource _pendingRequestsCts = new();
    private TimeProvider _timeProvider = TimeProvider.System;
    private readonly ILogger<DnsResolver> _logger;

    internal void SetTimeProvider(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    public DnsResolver(TimeProvider timeProvider, ILogger<DnsResolver> logger) : this(OperatingSystem.IsWindows() ? NetworkInfo.GetOptions() : ResolvConf.GetOptions())
    {
        _timeProvider = timeProvider;
        _logger = logger;
    }

    internal DnsResolver(ResolverOptions options)
    {
        _logger = NullLogger<DnsResolver>.Instance;
        _options = options;
        if (options.Servers.Length == 0)
        {
            throw new ArgumentException("There are no DNS servers configured.", nameof(options));
        }

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

        NameResolutionActivity activity = Telemetry.StartNameResolution(name, QueryType.SRV);
        SendQueryResult result = await SendQueryWithRetriesAsync(name, QueryType.SRV, cancellationToken).ConfigureAwait(false);

        if (result.Error is not SendQueryError.NoError)
        {
            Telemetry.StopNameResolution(activity, null, result.Error);
            return Array.Empty<ServiceResult>();
        }

        DnsResponse response = result.Response;

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
        Telemetry.StopNameResolution(activity, res, result.Error);
        return res;
    }

    public async ValueTask<AddressResult[]> ResolveIPAddressesAsync(string name, CancellationToken cancellationToken = default)
    {
        if (name == "localhost")
        {
            // name localhost exists outside of DNS and can't be resolved by a DNS server
            int len = (Socket.OSSupportsIPv4 ? 1 : 0) + (Socket.OSSupportsIPv6 ? 1 : 0);
            AddressResult[] res = new AddressResult[len];

            int index = 0;
            if (Socket.OSSupportsIPv6) // prefer IPv6
            {
                res[index] = new AddressResult(DateTime.MaxValue, IPAddress.IPv6Loopback);
            }
            if (Socket.OSSupportsIPv4)
            {
                res[index++] = new AddressResult(DateTime.MaxValue, IPAddress.Loopback);
            }
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

        if (name == "localhost")
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

        if (name.Length > MaximumNameLength)
        {
            throw new ArgumentException("Name is too long", nameof(name));
        }

        var queryType = addressFamily == AddressFamily.InterNetwork ? QueryType.A : QueryType.AAAA;
        NameResolutionActivity activity = Telemetry.StartNameResolution(name, queryType);
        SendQueryResult result = await SendQueryWithRetriesAsync(name, queryType, cancellationToken).ConfigureAwait(false);
        if (result.Error is not SendQueryError.NoError)
        {
            Telemetry.StopNameResolution(activity, null, result.Error);
            return Array.Empty<AddressResult>();
        }

        DnsResponse response = result.Response;
        var results = new List<AddressResult>(response.Answers.Count);

        // servers send back CNAME records together with associated A/AAAA records
        string currentAlias = name;

        foreach (var answer in response.Answers)
        {
            if (answer.Name != currentAlias)
            {
                continue;
            }

            if (answer.Type == QueryType.CNAME)
            {
                bool success = DnsPrimitives.TryReadQName(answer.Data.Span, 0, out currentAlias!, out _);
                Debug.Assert(success, "Failed to read CNAME");
                continue;
            }

            else if (answer.Type == queryType)
            {
                Debug.Assert(answer.Data.Length == IPv4Length || answer.Data.Length == IPv6Length);
                results.Add(new AddressResult(response.CreatedAt.AddSeconds(answer.Ttl), new IPAddress(answer.Data.Span)));
            }
        }

        AddressResult[] res = results.ToArray();
        Telemetry.StopNameResolution(activity, res, result.Error);
        return res;
    }

    internal struct SendQueryResult
    {
        public DnsResponse Response;
        public SendQueryError Error;
    }

    async ValueTask<SendQueryResult> SendQueryWithRetriesAsync(string name, QueryType queryType, CancellationToken cancellationToken)
    {
        SendQueryResult result = default;

        for (int index = 0; index < _options.Servers.Length; index++)
        {
            IPEndPoint serverEndPoint = _options.Servers[index];

            for (int attempt = 0; attempt < _options.Attempts; attempt++)
            {

                try
                {
                    result = await SendQueryToServerWithTimeoutAsync(serverEndPoint, name, queryType, index == _options.Servers.Length - 1, attempt, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
                {
                    Log.QueryError(_logger, queryType, name, serverEndPoint, attempt, ex);
                    continue; // retry or skip to the next server
                }

                switch (result.Error)
                {
                    case SendQueryError.NoError:
                        goto exit;
                    case SendQueryError.Timeout:
                        // TODO: should we retry on timeout or skip to the next server?
                        Log.Timeout(_logger, queryType, name, serverEndPoint, attempt);
                        break;
                    case SendQueryError.ServerError:
                        Log.ErrorResponseCode(_logger, queryType, name, serverEndPoint, result.Response.Header.ResponseCode);
                        break;
                    case SendQueryError.NoData:
                        Log.NoData(_logger, queryType, name, serverEndPoint, attempt);
                        break;
                }
            }
        }

    exit:
        // we have at least one server and we always keep the last received response.
        return result;
    }

    internal async ValueTask<SendQueryResult> SendQueryToServerWithTimeoutAsync(IPEndPoint serverEndPoint, string name, QueryType queryType, bool isLastServer, int attempt, CancellationToken cancellationToken)
    {
        (CancellationTokenSource cts, bool disposeTokenSource, CancellationTokenSource pendingRequestsCts) = PrepareCancellationTokenSource(cancellationToken);

        try
        {
            return await SendQueryToServerAsync(serverEndPoint, name, queryType, isLastServer, attempt, cts.Token).ConfigureAwait(false);
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

    async ValueTask<SendQueryResult> SendQueryToServerAsync(IPEndPoint serverEndPoint, string name, QueryType queryType, bool isLastServer, int attempt, CancellationToken cancellationToken)
    {
        Log.Query(_logger, queryType, name, serverEndPoint, attempt);

        DateTime queryStartedTime = _timeProvider.GetUtcNow().DateTime;
        (DnsDataReader responseReader, DnsMessageHeader header) = await SendDnsQueryCoreUdpAsync(serverEndPoint, name, queryType, cancellationToken).ConfigureAwait(false);

        try
        {
            if (header.IsResultTruncated)
            {
                Log.ResultTruncated(_logger, queryType, name, serverEndPoint, 0);
                responseReader.Dispose();
                // TCP fallback
                (responseReader, header) = await SendDnsQueryCoreTcpAsync(serverEndPoint, name, queryType, cancellationToken).ConfigureAwait(false);
            }

            if (header.QueryCount != 1 ||
                !responseReader.TryReadQuestion(out var qName, out var qType, out var qClass) ||
                qName != name || qType != queryType || qClass != QueryClass.Internet)
            {
                // TODO: do we care?
                throw new InvalidOperationException("Invalid response: Query mismatch");
                // return default;
            }

            if (header.ResponseCode != QueryResponseCode.NoError)
            {
                return new SendQueryResult
                {
                    Response = new DnsResponse(header, queryStartedTime, queryStartedTime, null!, null!, null!),
                    Error = SendQueryError.ServerError
                };
            }

            if (header.ResponseCode != QueryResponseCode.NoError && !isLastServer)
            {
                // we exhausted attempts on this server, try the next one
                responseReader.Dispose();
                return default;
            }

            int ttl = int.MaxValue;
            List<DnsResourceRecord> answers = ReadRecords(header.AnswerCount, ref ttl, ref responseReader);
            List<DnsResourceRecord> authorities = ReadRecords(header.AuthorityCount, ref ttl, ref responseReader);
            List<DnsResourceRecord> additionals = ReadRecords(header.AdditionalRecordCount, ref ttl, ref responseReader);

            DnsResponse response = new(header, queryStartedTime, queryStartedTime.AddSeconds(ttl), answers, authorities, additionals);
            responseReader.Dispose();

            return new SendQueryResult { Response = response, Error = ValidateResponse(response) };
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
                // copy the data to a new array since the underlying array is pooled
                records.Add(new DnsResourceRecord(record.Name, record.Type, record.Class, record.Ttl, record.Data.ToArray()));
            }

            return records;
        }
    }

    internal static bool GetNegativeCacheExpiration(in DnsResponse response, out DateTime expiration)
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

        DnsResourceRecord? soa = response.Authorities.FirstOrDefault(r => r.Type == QueryType.SOA);
        if (soa != null && DnsPrimitives.TryReadSoa(soa.Value.Data.Span, out string? mname, out string? rname, out uint serial, out uint refresh, out uint retry, out uint expire, out uint minimum, out _))
        {
            expiration = response.CreatedAt.AddSeconds(Math.Min(minimum, soa.Value.Ttl));
            return true;
        }

        expiration = default;
        return false;
    }

    internal static SendQueryError ValidateResponse(in DnsResponse response)
    {
        if (response.Header.ResponseCode == QueryResponseCode.NoError)
        {
            if (response.Answers.Count > 0)
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
            if (!response.Authorities.Any(r => r.Type == QueryType.NS) && GetNegativeCacheExpiration(response, out DateTime expiration))
            {
                // _cache.TryAdd(name, queryType, expiration, Array.Empty<T>());
            }
            return SendQueryError.NoData;
        }

        if (response.Header.ResponseCode == QueryResponseCode.NameError)
        {
            //
            // RFC 2308 Section 5 - Caching Negative Answers
            //
            //    A negative answer that resulted from a name error (NXDOMAIN) should
            //    be cached such that it can be retrieved and returned in response to
            //    another query for the same <QNAME, QCLASS> that resulted in the
            //    cached negative response.
            //
            if (GetNegativeCacheExpiration(response, out DateTime expiration))
            {
                // _cache.TryAddNonexistent(name, expiration);
            }

            return SendQueryError.ServerError;
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
            await socket.ConnectAsync(serverEndPoint, cancellationToken).ConfigureAwait(false);

            await socket.SendAsync(memory.Slice(0, length), SocketFlags.None, cancellationToken).ConfigureAwait(false);

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
                    // the message is not a response for our query.
                    // don't dispose reader, we will reuse the buffer
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

    internal static async ValueTask<(DnsDataReader reader, DnsMessageHeader header)> SendDnsQueryCoreTcpAsync(IPEndPoint serverEndPoint, string name, QueryType queryType, CancellationToken cancellationToken)
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

                if (responseLength < 0 && bytesRead >= 2)
                {
                    responseLength = BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(0, 2));

                    if (responseLength > buffer.Length)
                    {
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
                throw new InvalidOperationException("Invalid response: Header mismatch");
            }

            buffer = null!;
            return (responseReader, header);
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
        DnsMessageHeader header = default;
        header.InitQueryHeader();
        DnsDataWriter writer = new DnsDataWriter(buffer);
        if (!writer.TryWriteHeader(header) ||
            !writer.TryWriteQuestion(name, queryType, QueryClass.Internet))
        {
            // should never happen since we validated the name length
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
}
