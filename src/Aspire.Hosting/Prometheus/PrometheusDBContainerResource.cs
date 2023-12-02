using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

public class PrometheusContainerResource : ContainerResource
{
    public string ConfigFilePath { get; init; }
    public string DataVolumeName { get; init; }

    public PrometheusContainerResource(string name, string configFilePath, string dataVolumeName) : base(name)
    {
        ConfigFilePath = configFilePath;
        DataVolumeName = dataVolumeName;
    }

}
