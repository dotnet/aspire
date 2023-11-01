namespace MetricsApp;

public static class WeatherApi
{
    private static readonly string[] s_summaries =
    [
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    ];

    public static void MapWeatherApi(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("weather", async () =>
        {
            await Task.Delay(Random.Shared.Next(1000));

            var results = Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = s_summaries[Random.Shared.Next(s_summaries.Length)]
            }).ToArray();
            return results;
        }).RequireAuthorization();
    }

    private sealed class WeatherForecast
    {
        public DateOnly Date { get; set; }
        public int TemperatureC { get; set; }
        public string? Summary { get; set; }
        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
    }
}
