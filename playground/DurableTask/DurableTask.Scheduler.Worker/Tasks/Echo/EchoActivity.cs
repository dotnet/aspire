using System.Net.Http.Json;
using Microsoft.DurableTask;

namespace DurableTask.Scheduler.Worker.Tasks.Echo;

sealed class EchoActivity(IHttpClientFactory clientFactory) : TaskActivity<string, string>
{
    public override async Task<string> RunAsync(TaskActivityContext context, string input)
    {
        HttpClient client = clientFactory.CreateClient("Echo");

        var result = await client.PostAsync("/echo", JsonContent.Create(new EchoInput { Text = input }));

        var output = await result.Content.ReadFromJsonAsync<EchoInput>();

        return output?.Text ?? throw new InvalidOperationException("Invalid response from echo service!");
    }
}
