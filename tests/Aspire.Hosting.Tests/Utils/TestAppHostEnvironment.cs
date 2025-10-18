// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Aspire.Hosting.Tests.Utils;

internal sealed class TestAppHostEnvironment : IAppHostEnvironment
{
    private readonly IConfiguration? _configuration;
    private readonly IHostEnvironment? _hostEnvironment;

    public TestAppHostEnvironment(IConfiguration? configuration = null, IHostEnvironment? hostEnvironment = null)
    {
        _configuration = configuration;
        _hostEnvironment = hostEnvironment;
    }

    public string ProjectName => _configuration?["AppHost:DashboardApplicationName"] ?? _hostEnvironment?.ApplicationName ?? "TestApp";
    public string ProjectDirectory => _configuration?["AppHost:Directory"] ?? "/test";
    public string FullPath => _configuration?["AppHost:Path"] ?? "/test/TestApp";
    public string DashboardApplicationName => _configuration?["AppHost:DashboardApplicationName"] ?? _hostEnvironment?.ApplicationName ?? "TestApp";
    public string DefaultHash => _configuration?["AppHost:Sha256"] ?? "0000000000000000000000000000000000000000000000000000000000000000";
    public string FullPathHash => _configuration?["AppHost:PathSha256"] ?? "0000000000000000000000000000000000000000000000000000000000000000";
    public string ProjectNameHash => _configuration?["AppHost:ProjectNameSha256"] ?? "0000000000000000000000000000000000000000000000000000000000000000";
    public string? ContainerHostname => _configuration?["AppHost:ContainerHostname"];
    public string? DefaultLaunchProfileName => _configuration?["AppHost:DefaultLaunchProfileName"];
    public string? OtlpApiKey => _configuration?["AppHost:OtlpApiKey"];
    public string? ResourceServiceApiKey => _configuration?["AppHost:ResourceService:ApiKey"];
    public string? ResourceServiceAuthMode => _configuration?["AppHost:ResourceService:AuthMode"];
}
