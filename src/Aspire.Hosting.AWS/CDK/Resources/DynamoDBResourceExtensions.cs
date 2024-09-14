// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amazon.CDK.AWS.DynamoDB;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.AWS;
using Aspire.Hosting.AWS.CDK;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding Amazon DynamoDB resources to the application model.
/// </summary>
public static class DynamoDBResourceExtensions
{

    private const string TableNameOutputName = "TableName";

    /// <summary>
    /// Adds an Amazon DynamoDB table.
    /// </summary>
    /// <param name="builder">The builder for the AWS CDK stack.</param>
    /// <param name="name">The name of the resource.</param>
    /// <param name="props">The properties of the table.</param>
    public static IResourceBuilder<IConstructResource<Table>> AddDynamoDBTable(this IResourceBuilder<IStackResource> builder, string name, ITableProps props)
    {
        return builder.AddConstruct(name, scope => new Table(scope, name, props));
    }

    /// <summary>
    /// Adds an global secondary index to the table.
    /// </summary>
    /// <param name="builder">The builder for the table resource.</param>
    /// <param name="props">The properties for the global secondary index.</param>
    public static IResourceBuilder<IConstructResource<Table>> AddGlobalSecondaryIndex(this IResourceBuilder<IConstructResource<Table>> builder, IGlobalSecondaryIndexProps props)
    {
        builder.Resource.Construct.AddGlobalSecondaryIndex(props);
        return builder;
    }

    /// <summary>
    /// Adds a local secondary index to the table.
    /// </summary>
    /// <param name="builder">The builder for the table resource.</param>
    /// <param name="props">The properties for the local secondary index.</param>
    public static IResourceBuilder<IConstructResource<Table>> AddLocalSecondaryIndex(this IResourceBuilder<IConstructResource<Table>> builder, ILocalSecondaryIndexProps props)
    {
        builder.Resource.Construct.AddLocalSecondaryIndex(props);
        return builder;
    }

    /// <summary>
    /// Adds a reference of an Amazon DynamoDB table to a project. The output parameters of the table are added to the project IConfiguration.
    /// </summary>
    /// <param name="builder">The builder for the resource.</param>
    /// <param name="table">The Amazon DynamoDB table resource.</param>
    /// <param name="configSection">The optional config section in IConfiguration to add the output parameters.</param>
    public static IResourceBuilder<TDestination> WithReference<TDestination>(this IResourceBuilder<TDestination> builder, IResourceBuilder<IConstructResource<Table>> table, string? configSection = null)
        where TDestination : IResourceWithEnvironment
    {
        configSection ??= $"{Constants.DefaultConfigSection}:{table.Resource.Name}";
        var prefix = configSection.ToEnvironmentVariables();
        return builder.WithEnvironment($"{prefix}__{TableNameOutputName}", table, t => t.TableName, TableNameOutputName);
    }
}
