using Amazon.CDK.AWS.DynamoDB;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.AWS;
using Aspire.Hosting.AWS.CDK;

namespace Aspire.Hosting;

/// <summary>
///
/// </summary>
public static class DynamoDBResourceExtensions
{
    /// <summary>
    ///
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="name"></param>
    /// <param name="props"></param>
    /// <returns></returns>
    public static IResourceBuilder<IConstructResource<Table>> AddDynamoDBTable(this IResourceBuilder<IResourceWithConstruct> builder, string name, ITableProps props)
    {
        return builder.AddConstruct(name, scope => new Table(scope, name, props));
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="props"></param>
    /// <returns></returns>
    public static IResourceBuilder<IConstructResource<Table>> AddGlobalSecondaryIndex(this IResourceBuilder<IConstructResource<Table>> builder, IGlobalSecondaryIndexProps props)
    {
        builder.Resource.Construct.AddGlobalSecondaryIndex(props);
        return builder;
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="table"></param>
    /// <param name="configSection"></param>
    /// <returns></returns>
    public static IResourceBuilder<TDestination> WithReference<TDestination>(this IResourceBuilder<TDestination> builder, IResourceBuilder<IConstructResource<Table>> table, string configSection = Constants.DefaultConfigSection)
        where TDestination : IResourceWithEnvironment
    {
        var prefix = configSection.Replace(':', '_');
        return builder.WithEnvironment($"{prefix}__TableName", table, t => t.TableName);
    }
}
