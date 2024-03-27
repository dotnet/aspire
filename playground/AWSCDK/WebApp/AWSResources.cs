// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace WebApp;

public class AWSResources
{
    public TableResource? Table { get; set; }
}

public class TableResource
{
    public string? TableName { get; set; }
}
