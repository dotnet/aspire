export interface WeatherForecast {
    date: string;
    temperatureC: number;
    temperatureF: number;
    summary: string;
}

export type WeatherForecasts = WeatherForecast[];