// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace AzureKusto.Worker;

public class WorkerOptions
{
    public string DatabaseName { get; } = "testdb";

    public string TableName { get; } = "TestTable";

    public bool IsIngestionComplete { get; set; }
}
