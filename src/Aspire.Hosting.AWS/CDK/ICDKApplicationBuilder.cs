using Amazon.CDK;

namespace Aspire.Hosting.AWS.CDK;

/// <summary>
/// AWS CDK enabled version of the <see cref="IDistributedApplicationBuilder"/>.
/// </summary>
public interface ICDKApplicationBuilder : IDistributedApplicationBuilder
{
    /// <summary>
    ///
    /// </summary>
    App App { get; }
}
