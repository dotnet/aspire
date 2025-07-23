using System.Text.Json.Serialization;

namespace DurableTask.Scheduler.Worker.Tasks.Echo;

sealed record EchoInput
{
    [JsonPropertyName("text")]
    public required string Text { get; init; }
}
