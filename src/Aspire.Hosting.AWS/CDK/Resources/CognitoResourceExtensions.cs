using Amazon.CDK.AWS.Cognito;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.AWS.CDK;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding Amazon Cognito resources to the application model.
/// </summary>
public static class CognitoResourceExtensions
{
    /// <summary>
    /// Adds an Amazon Cognito user pool.
    /// </summary>
    /// <param name="builder">The builder for the distributed application.</param>
    /// <param name="name">The name of the resource.</param>
    /// <param name="props">The properties of the userpool.</param>
    public static IResourceBuilder<IConstructResource<UserPool>> AddCognitoUserPool(this IResourceBuilder<IResourceWithConstruct> builder, string name, IUserPoolProps? props = null)
    {
        return builder.AddConstruct(name, scope => new UserPool(scope, name, props));
    }

    /// <summary>
    /// Adds an Amazon Cognito user pool client.
    /// </summary>
    /// <param name="builder">The builder for the user pool.</param>
    /// <param name="name">the name of the resource.</param>
    /// <param name="options">The options of the client.</param>
    public static IResourceBuilder<IConstructResource<UserPoolClient>> AddClient(this IResourceBuilder<IConstructResource<UserPool>> builder, string name, IUserPoolClientOptions? options)
    {
        return builder.AddConstruct(name, scope => builder.Resource.Construct.AddClient(name, options));
    }
}
