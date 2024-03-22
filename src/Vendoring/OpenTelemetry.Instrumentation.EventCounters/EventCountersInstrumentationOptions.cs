// <copyright file="EventCountersInstrumentationOptions.cs" company="OpenTelemetry Authors">
// Copyright The OpenTelemetry Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenTelemetry.Instrumentation.EventCounters;

/// <summary>
/// EventCounters Instrumentation Options.
/// </summary>
internal class EventCountersInstrumentationOptions
{
    internal readonly HashSet<string> EventSourceNames = new();

    /// <summary>
    /// Gets or sets the subscription interval in seconds for reading values
    /// from the configured EventCounters.
    /// </summary>
    public int RefreshIntervalSecs { get; set; } = 1;

    /// <summary>
    /// Listens to EventCounters from the given EventSource name.
    /// </summary>
    /// <param name="names">The EventSource names to listen to.</param>
    public void AddEventSources(params string[] names)
    {
        if (names.Contains("System.Runtime"))
        {
            throw new NotSupportedException("Use the `OpenTelemetry.Instrumentation.Runtime` or `OpenTelemetry.Instrumentation.Process` instrumentations.");
        }

        this.EventSourceNames.UnionWith(names);
    }

    /// <summary>
    /// Returns whether or not an EventSource should be enabled on the EventListener.
    /// </summary>
    /// <param name="eventSourceName">The EventSource name.</param>
    /// <returns><c>true</c> when an EventSource with the name <paramref name="eventSourceName"/> should be enabled.</returns>
    internal bool ShouldListenToSource(string eventSourceName)
    {
        return this.EventSourceNames.Contains(eventSourceName);
    }
}
