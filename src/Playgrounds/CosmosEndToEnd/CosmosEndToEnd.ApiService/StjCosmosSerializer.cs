// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Azure.Core.Serialization;
using Microsoft.Azure.Cosmos;

namespace CosmosEndToEnd.ApiService;

public class StjSerializer: CosmosSerializer
{
    private readonly JsonObjectSerializer _systemTextJsonSerializer;

    public StjSerializer(JsonSerializerOptions jsonSerializerOptions)
    {
        this._systemTextJsonSerializer = new JsonObjectSerializer(jsonSerializerOptions);
    }

    public override T FromStream<T>(Stream stream)
    {
        using (stream)
        {
            if (stream.CanSeek
                    && stream.Length == 0)
            {
                return default!;
            }

            if (typeof(Stream).IsAssignableFrom(typeof(T)))
            {
                return (T)(object)stream;
            }

            return (T)this._systemTextJsonSerializer.Deserialize(stream, typeof(T), default)!;
        }
    }

    public override Stream ToStream<T>(T input)
    {
        MemoryStream streamPayload = new MemoryStream();
        this._systemTextJsonSerializer.Serialize(streamPayload, input, input.GetType(), default);
        streamPayload.Position = 0;
        return streamPayload;
    }
}
