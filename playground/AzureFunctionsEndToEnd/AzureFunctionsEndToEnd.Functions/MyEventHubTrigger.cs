using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AspirePlusFunctions.Functions;

public class MyEventHubTrigger
{
    private readonly ILogger<MyEventHubTrigger> _logger;

    public MyEventHubTrigger(ILogger<MyEventHubTrigger> logger)
    {
        _logger = logger;
    }

    [Function(nameof(MyEventHubTrigger))]
    public void Run([EventHubTrigger("myhub", Connection = "eventhubs")] string[] input)
    {
        _logger.LogInformation($"C# EventHub trigger function processed: {input.Length} messages");
    }
}

