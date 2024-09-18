// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;

namespace Confluent.Kafka;

/// <summary>
/// An asynchronous action to process the <see cref="ConsumeResult{TKey,TValue}"/>.
/// </summary>
/// <param name="consumeResult">The <see cref="ConsumeResult{TKey,TValue}"/>.</param>
/// <param name="activity">The <see cref="Activity"/>.</param>
/// <param name="cancellationToken">An optional <see cref="CancellationToken"/>.</param>
/// <typeparam name="TKey">The type of key of the <see cref="ConsumeResult{TKey,TValue}"/>.</typeparam>
/// <typeparam name="TValue">The type of value of the <see cref="ConsumeResult{TKey,TValue}"/>.</typeparam>
/// <returns>A <see cref="ValueTask"/>.</returns>
internal delegate ValueTask OpenTelemetryConsumeAndProcessMessageHandler<TKey, TValue>(
    ConsumeResult<TKey, TValue> consumeResult,
    Activity? activity,
    CancellationToken cancellationToken = default);
