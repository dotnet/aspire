using Amazon.CDK.AWS.Cognito;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.AWS;
using Aspire.Hosting.AWS.CDK;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding Amazon Cognito resources to the application model.
/// </summary>
public static class CognitoResourceExtensions
{

    private const string UserPoolIdOutputName = "UserPoolId";

    /// <summary>
    /// Adds an Amazon Cognito user pool.
    /// </summary>
    /// <param name="builder">The builder for the AWS CDK stack.</param>
    /// <param name="name">The name of the resource.</param>
    /// <param name="props">The properties of the userpool.</param>
    public static IResourceBuilder<IConstructResource<UserPool>> AddCognitoUserPool(this IResourceBuilder<IStackResource> builder, string name, IUserPoolProps? props = null)
    {
        return builder.AddConstruct(name, scope => new UserPool(scope, name, props));
    }

    /// <summary>
    /// Adds an Amazon Cognito user pool client.
    /// </summary>
    /// <param name="builder">The builder for the user pool.</param>
    /// <param name="name">the name of the resource.</param>
    /// <param name="options">The options of the client.</param>
    public static IResourceBuilder<IConstructResource<UserPoolClient>> AddClient(this IResourceBuilder<IConstructResource<UserPool>> builder, string name, IUserPoolClientOptions? options = null)
    {
        return builder.AddConstruct(name, _ => builder.Resource.Construct.AddClient(name, options));
    }

    /// <summary>
    /// Adds a reference of an Amazon Cognito user pool to a project. The output parameters of the user pool are added to the project IConfiguration.
    /// </summary>
    /// <param name="builder">The builder for the resource.</param>
    /// <param name="userPool">The Amazon Cognito user pool resource.</param>
    /// <param name="configSection">The optional config section in IConfiguration to add the output parameters.</param>
    public static IResourceBuilder<TDestination> WithReference<TDestination>(this IResourceBuilder<TDestination> builder, IResourceBuilder<IConstructResource<UserPool>> userPool, string? configSection = null)
        where TDestination : IResourceWithEnvironment
    {
        configSection ??= $"{Constants.DefaultConfigSection}:{userPool.Resource.Name}";
        var prefix = configSection.ToEnvironmentVariables();
        return builder.WithEnvironment($"{prefix}__{UserPoolIdOutputName}", userPool, p => p.UserPoolId, UserPoolIdOutputName);
    }
}
