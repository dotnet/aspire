// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http.Json;
using Xunit;

namespace Aspire.Hosting.Testing.Tests;

public class TestingHarnessTests(DistributedApplicationFixture<Program> appHostFixture) : IClassFixture<DistributedApplicationFixture<Program>>
{
    private readonly HttpClient _httpClient = appHostFixture.CreateHttpClient("mywebapp1");

    [Fact]
    public async Task HttpClientGetTest()
    {
        var result1 = await _httpClient.GetFromJsonAsync<WeatherForecast[]>("/weatherforecast");
        Assert.NotNull(result1);
        Assert.True(result1.Length > 0);
    }

    private sealed record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
    {
        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
    }
}
