using Aspire.Hosting.Pipelines;

namespace Pipelines.Library;

public static class DistributedApplicationPipelineExtensions
{
    public static IDistributedApplicationPipeline AddAppServiceZipDeploy(this IDistributedApplicationPipeline pipeline)
    {
        return pipeline;
    }
}
