#if !SKIP_EVENTHUBS_EMULATION
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AspirePlusFunctions.Functions;

public class MyEventHubTrigger(ILogger<MyEventHubTrigger> logger)
{
    [Function(nameof(MyEventHubTrigger))]
    public void Run([EventHubTrigger("myhub", Connection = "eventhubs")] string[] input)
    {
        logger.LogInformation("C# EventHub trigger function processed: {Count} messages", input.Length);
    }
}
#endif
