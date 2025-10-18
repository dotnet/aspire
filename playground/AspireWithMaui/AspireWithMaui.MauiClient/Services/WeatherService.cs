using System.Net.Http.Json;

namespace AspireWithMaui.MauiClient.Services;

public class WeatherService : IWeatherService
{
    private readonly HttpClient _httpClient;

    public WeatherService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<WeatherForecast[]> GetWeatherForecastAsync()
    {
        try
        {
            // Make request to the weather API via service discovery
            var response = await _httpClient.GetFromJsonAsync<WeatherForecast[]>("WeatherForecast");
            return response ?? Array.Empty<WeatherForecast>();
        }
        catch (Exception ex)
        {
            // In a real app, you'd want better error handling
            System.Diagnostics.Debug.WriteLine($"Error getting weather: {ex.Message}");
            return Array.Empty<WeatherForecast>();
        }
    }
}