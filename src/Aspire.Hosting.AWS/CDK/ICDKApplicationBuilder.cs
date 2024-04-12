using Amazon.CDK;

namespace Aspire.Hosting.AWS.CDK;

/// <summary>
///
/// </summary>
public interface ICDKApplicationBuilder : IDistributedApplicationBuilder
{
    /// <summary>
    ///
    /// </summary>
    App App { get; }
}
