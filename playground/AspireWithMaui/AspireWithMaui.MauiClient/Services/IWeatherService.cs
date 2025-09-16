namespace AspireWithMaui.MauiClient.Services;

public interface IWeatherService
{
    Task<WeatherForecast[]> GetWeatherForecastAsync();
}