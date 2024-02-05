// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using System.Text.RegularExpressions;
using Aspire.Hosting.MySql;
using Aspire.Hosting.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aspire.Hosting.Tests.MySql;

[Collection("IntegrationServices")]
public class MySqlFunctionalTests
{
    private readonly IntegrationServicesFixture _integrationServicesFixture;

    public MySqlFunctionalTests(IntegrationServicesFixture integrationServicesFixture)
    {
        _integrationServicesFixture = integrationServicesFixture;
    }

    [LocalOnlyFact()]
    public async Task VerifyMySqlWorks()
    {
        // MySql health check reports healthy during temporary server phase, c.f. https://github.com/Xabaril/AspNetCore.Diagnostics.HealthChecks/issues/2031
        // This is mitigated by standard resilience handlers in the IntegrationServicesFixture HttpClient configuration

        var testProgram = _integrationServicesFixture.TestProgram;
        var client = _integrationServicesFixture.HttpClient;

        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));

        var response = await testProgram.IntegrationServiceABuilder!.HttpGetAsync(client, "http", "/mysql/verify", cts.Token);
        var responseContent = await response.Content.ReadAsStringAsync();

        Assert.True(response.IsSuccessStatusCode, responseContent);
    }

    [Fact]
    public void WithMySqlTwiceEndsUpWithOneAdminContainer()
    {
        var builder = DistributedApplication.CreateBuilder();
        builder.AddMySql("mySql").WithPhpMyAdmin();
        builder.AddMySqlContainer("mySql2").WithPhpMyAdmin();

        Assert.Single(builder.Resources.OfType<PhpMyAdminContainerResource>());
    }

    [Fact]
    public async Task SingleMySqlInstanceProducesCorrectMySqlHostsVariable()
    {
        var builder = DistributedApplication.CreateBuilder();
        var mysql = builder.AddMySql("mySql").WithPhpMyAdmin();
        var app = builder.Build();

        // Add fake allocated endpoints.
        mysql.WithAnnotation(new AllocatedEndpointAnnotation("tcp", ProtocolType.Tcp, "host.docker.internal", 5001, "tcp"));

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();
        var hook = new PhpMyAdminConfigWriterHook();
        await hook.AfterEndpointsAllocatedAsync(model, CancellationToken.None);

        var myAdmin = builder.Resources.Single(r => r.Name.EndsWith("-phpmyadmin"));

        var envAnnotations = myAdmin.Annotations.OfType<EnvironmentCallbackAnnotation>();

        var config = new Dictionary<string, string>();
        var context = new EnvironmentCallbackContext("dcp", config);

        foreach (var annotation in envAnnotations)
        {
            annotation.Callback(context);
        }

        Assert.Equal("host.docker.internal:5001", context.EnvironmentVariables["PMA_HOST"]);
        Assert.NotNull(context.EnvironmentVariables["PMA_USER"]);
        Assert.NotNull(context.EnvironmentVariables["PMA_PASSWORD"]);
    }

    [Fact]
    public void WithPhpMyAdminAddsContainer()
    {
        var builder = DistributedApplication.CreateBuilder();
        builder.AddMySql("mySql").WithPhpMyAdmin();

        var container = builder.Resources.Single(r => r.Name == "mySql-phpmyadmin");
        var volume = container.Annotations.OfType<VolumeMountAnnotation>().Single();

        Assert.True(File.Exists(volume.Source)); // File should exist, but will be empty.
        Assert.Equal("/etc/phpmyadmin/config.user.inc.php", volume.Target);
    }

    [Fact]
    public void WithPhpMyAdminProducesValidServerConfigFile()
    {
        var builder = DistributedApplication.CreateBuilder();
        var mysql1 = builder.AddMySql("mysql1").WithPhpMyAdmin(8081);
        var mysql2 = builder.AddMySql("mysql2").WithPhpMyAdmin(8081);

        // Add fake allocated endpoints.
        mysql1.WithAnnotation(new AllocatedEndpointAnnotation("tcp", ProtocolType.Tcp, "host.docker.internal", 5001, "tcp"));
        mysql2.WithAnnotation(new AllocatedEndpointAnnotation("tcp", ProtocolType.Tcp, "host.docker.internal", 5002, "tcp"));

        var myAdmin = builder.Resources.Single(r => r.Name.EndsWith("-phpmyadmin"));
        var volume = myAdmin.Annotations.OfType<VolumeMountAnnotation>().Single();

        var app = builder.Build();
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var hook = new PhpMyAdminConfigWriterHook();
        hook.AfterEndpointsAllocatedAsync(appModel, CancellationToken.None);

        using var stream = File.OpenRead(volume.Source);
        var fileContents = new StreamReader(stream).ReadToEnd();

        // check to see that the two hosts are in the file
        string pattern1 = @"\$cfg\['Servers'\]\[\$i\]\['host'\] = 'host.docker.internal:5001';";
        string pattern2 = @"\$cfg\['Servers'\]\[\$i\]\['host'\] = 'host.docker.internal:5002';";
        Match match1 = Regex.Match(fileContents, pattern1);
        Assert.True(match1.Success);
        Match match2 = Regex.Match(fileContents, pattern2);
        Assert.True(match2.Success);
    }
}
