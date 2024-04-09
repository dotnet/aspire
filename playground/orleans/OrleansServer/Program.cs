using Orleans.Runtime;
using OrleansContracts;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddKeyedAzureTableClient("clustering");
builder.AddKeyedAzureBlobClient("grainstate");
builder.UseOrleans();

var app = builder.Build();

app.MapGet("/", () => "OK");

await app.RunAsync();

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

