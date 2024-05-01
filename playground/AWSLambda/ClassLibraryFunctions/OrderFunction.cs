using Amazon.Lambda.SQSEvents;

namespace ClassLibraryFunctions;

public class OrderFunction
{
    public Task<string> HandleAsync(SQSEvent @event)
    {
        return Task.FromResult("Order is now processed");
    }
}
