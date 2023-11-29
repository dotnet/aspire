using Aspire.Orleans.Server;
using Microsoft.Extensions.Hosting;
using Orleans.Runtime;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();
builder.UseAspireOrleansServer();

using var host = builder.Build();

await host.RunAsync();

public sealed class CounterGrain(
    [PersistentState("count")] IPersistentState<int> count) : ICounterGrain
{
    public ValueTask<int> Get()
    {
        return ValueTask.FromResult(count.State);
    }

    public async ValueTask<int> Increment()
    {
        var result = ++count.State;
        await count.WriteStateAsync();
        return result;
    }
}

