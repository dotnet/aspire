using Amazon.CDK.AWS.Cognito;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.AWS.CDK;

namespace Aspire.Hosting;

/// <summary>
///
/// </summary>
public static class CognitoResourceExtensions
{
    /// <summary>
    ///
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="name"></param>
    /// <param name="props"></param>
    /// <returns></returns>
    public static IResourceBuilder<IConstructResource<UserPool>> AddCognitoUserPool(this IResourceBuilder<IResourceWithConstruct> builder, string name, IUserPoolProps props)
    {
        return builder.AddConstruct(name, scope => new UserPool(scope, name, props));
    }
}
