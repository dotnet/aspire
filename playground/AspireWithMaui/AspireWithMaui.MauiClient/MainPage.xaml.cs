using System.Collections.ObjectModel;
using AspireWithMaui.MauiClient.Services;

namespace AspireWithMaui.MauiClient;

public partial class MainPage : ContentPage
{
	private readonly IWeatherService _weatherService;
	public ObservableCollection<WeatherForecast> WeatherData { get; set; } = new();

	public MainPage(IWeatherService weatherService)
	{
		_weatherService = weatherService;
		InitializeComponent();
		BindingContext = this;
	}

	private async void OnLoadWeatherClicked(object? sender, EventArgs e)
	{
		try
		{
			StatusLabel.Text = "Loading weather data...";
			LoadWeatherBtn.IsEnabled = false;

			var weatherData = await _weatherService.GetWeatherForecastAsync();
			
			WeatherData.Clear();
			foreach (var item in weatherData)
			{
				WeatherData.Add(item);
			}

			StatusLabel.Text = weatherData.Length > 0 
				? $"Loaded {weatherData.Length} weather forecasts" 
				: "No weather data available";
		}
		catch (Exception ex)
		{
			StatusLabel.Text = $"Error: {ex.Message}";
		}
		finally
		{
			LoadWeatherBtn.IsEnabled = true;
		}
	}

}
