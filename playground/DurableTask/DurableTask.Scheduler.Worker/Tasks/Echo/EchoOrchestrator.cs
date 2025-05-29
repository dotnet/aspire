using Microsoft.DurableTask;

namespace DurableTask.Scheduler.Worker.Tasks.Echo;

sealed class EchoOrchestrator : TaskOrchestrator<EchoInput, string>
{
    public override async Task<string> RunAsync(TaskOrchestrationContext context, EchoInput input)
    {
        string output = await context.CallActivityAsync<string>("EchoActivity", input.Text);

        output = await context.CallActivityAsync<string>("EchoActivity", output);

        output = await context.CallActivityAsync<string>("EchoActivity", output);

        return output;
    }
}
