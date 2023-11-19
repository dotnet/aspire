// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Aspire.Confluent.Kafka;

/// <summary>
/// Maps to the JSON output returned by the <see href="https://github.com/confluentinc/librdkafka/blob/master/STATISTICS.md">librdkafka statistics API</see>. 
/// </summary>
internal sealed class Statistics
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    [JsonPropertyName("client_id")]
    public string? ClientId { get; set; }
    [JsonPropertyName("type")]
    public string? Type { get; set; }
    [JsonPropertyName("ts")]
    public long Timestamp { get; set; }
    [JsonPropertyName("time")]
    public long Time { get; set; }
    [JsonPropertyName("age")]
    public long Age { get; set; }
    [JsonPropertyName("replyq")]
    public long ReplyQueue { get; set; }
    [JsonPropertyName("msg_cnt")]
    public long MessageCount { get; set; }
    [JsonPropertyName("msg_size")]
    public long MessageSize { get; set; }
    [JsonPropertyName("msg_max")]
    public long MessageMax { get; set; }
    [JsonPropertyName("msg_size_max")]
    public long MessageSizeMax { get; set; }
    [JsonPropertyName("tx")]
    public long Tx { get; set; }
    [JsonPropertyName("tx_bytes")]
    public long TxBytes { get; set; }
    [JsonPropertyName("rx")]
    public long Rx { get; set; }
    [JsonPropertyName("rx_bytes")]
    public long RxBytes { get; set; }
    [JsonPropertyName("txmsgs")]
    public long TxMessages { get; set; }
    [JsonPropertyName("txmsg_bytes")]
    public long TxMessageBytes { get; set; }
    [JsonPropertyName("rxmsgs")]
    public long RxMessages { get; set; }
    [JsonPropertyName("rxmsg_bytes")]
    public long RxMessageBytes { get; set; }
    [JsonPropertyName("simple_cnt")]
    public long SimpleCount { get; set; }
    [JsonPropertyName("metadata_cache_cnt")]
    public long MetadataCacheCount { get; set; }
}

[JsonSerializable(typeof(Statistics))]
[JsonSourceGenerationOptions]
internal sealed partial class StatisticsJsonSerializerContext : JsonSerializerContext
{

}
