// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Aspire.Hosting;

/// <summary>
/// Provides information about the AppHost environment.
/// </summary>
internal sealed class AppHostEnvironment : IAppHostEnvironment
{
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _hostEnvironment;

    /// <summary>
    /// Initializes a new instance of the <see cref="AppHostEnvironment"/> class.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    /// <param name="hostEnvironment">The host environment.</param>
    public AppHostEnvironment(IConfiguration configuration, IHostEnvironment hostEnvironment)
    {
        _configuration = configuration;
        _hostEnvironment = hostEnvironment;
    }

    /// <inheritdoc />
    public string ProjectName => _configuration["AppHost:DashboardApplicationName"] ?? _hostEnvironment.ApplicationName;

    /// <inheritdoc />
    public string Directory => _configuration["AppHost:Directory"]!;

    /// <inheritdoc />
    public string Path => _configuration["AppHost:Path"]!;

    /// <inheritdoc />
    public string DashboardApplicationName => _configuration["AppHost:DashboardApplicationName"] ?? _hostEnvironment.ApplicationName;

    /// <inheritdoc />
    public string Sha256 => _configuration["AppHost:Sha256"]!;

    /// <inheritdoc />
    public string PathSha256 => _configuration["AppHost:PathSha256"]!;

    /// <inheritdoc />
    public string ProjectNameSha256 => _configuration["AppHost:ProjectNameSha256"]!;

    /// <inheritdoc />
    public string? ContainerHostname => _configuration["AppHost:ContainerHostname"];

    /// <inheritdoc />
    public string? DefaultLaunchProfileName => _configuration["AppHost:DefaultLaunchProfileName"];

    /// <inheritdoc />
    public string? OtlpApiKey => _configuration["AppHost:OtlpApiKey"];

    /// <inheritdoc />
    public string? BrowserToken => _configuration["AppHost:BrowserToken"];

    /// <inheritdoc />
    public string? ResourceServiceApiKey => _configuration["AppHost:ResourceService:ApiKey"];

    /// <inheritdoc />
    public string? ResourceServiceAuthMode => _configuration["AppHost:ResourceService:AuthMode"];
}
