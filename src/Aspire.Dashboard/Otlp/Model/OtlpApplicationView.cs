// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Aspire.Dashboard.Model.Otlp;
using Aspire.Dashboard.Otlp.Storage;
using Google.Protobuf.Collections;
using OpenTelemetry.Proto.Common.V1;

namespace Aspire.Dashboard.Otlp.Model;

[DebuggerDisplay("Application = {Application}, Properties = {Properties.Count}")]
public class OtlpApplicationView
{
    public ApplicationKey ApplicationKey => Application.ApplicationKey;
    public OtlpApplication Application { get; }
    public KeyValuePair<string, string>[] Properties { get; }

    public OtlpApplicationView(OtlpApplication application, RepeatedField<KeyValue> attributes)
    {
        Application = application;

        var properties = attributes.ToKeyValuePairs(application.Context, filter: attribute =>
        {
            switch (attribute.Key)
            {
                case OtlpApplication.SERVICE_NAME:
                case OtlpApplication.SERVICE_INSTANCE_ID:
                    // Explicitly ignore these
                    return false;
                default:
                    return true;
            }
        });

        // Sort so keys are in a consistent order for equality check.
        Array.Sort(properties, (p1, p2) => string.Compare(p1.Key, p2.Key, StringComparisons.OtlpAttribute));

        Properties = properties;
    }

    public List<OtlpDisplayField> AllProperties()
    {
        var props = new List<OtlpDisplayField>
        {
            new OtlpDisplayField { DisplayName = "service.name", Key = KnownResourceFields.ServiceNameField, Value = Application.ApplicationName },
            new OtlpDisplayField { DisplayName = "service.instance.id", Key = KnownResourceFields.ServiceInstanceIdField, Value = Application.InstanceId }
        };

        foreach (var kv in Properties)
        {
            props.Add(new OtlpDisplayField { DisplayName = kv.Key, Key = $"unknown-{kv.Key}", Value = kv.Value });
        }

        return props;
    }
}
