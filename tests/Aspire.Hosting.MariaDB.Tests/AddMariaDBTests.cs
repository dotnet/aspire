// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using System.Text.RegularExpressions;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Tests.Utils;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aspire.Hosting.MariaDB.Tests;

public class AddMariaDBTests
{
    [Fact]
    public void AddMariaDBAddsGeneratedPasswordParameterWithUserSecretsParameterDefaultInRunMode()
    {
        using var appBuilder = TestDistributedApplicationBuilder.Create();

        var mariadb = appBuilder.AddMariaDB("mariadb");

        Assert.Equal("Aspire.Hosting.ApplicationModel.UserSecretsParameterDefault", mariadb.Resource.PasswordParameter.Default?.GetType().FullName);
    }

    [Fact]
    public void AddMariaDBDoesNotAddGeneratedPasswordParameterWithUserSecretsParameterDefaultInPublishMode()
    {
        using var appBuilder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var mariadb = appBuilder.AddMariaDB("mariadb");

        Assert.NotEqual("Aspire.Hosting.ApplicationModel.UserSecretsParameterDefault", mariadb.Resource.PasswordParameter.Default?.GetType().FullName);
    }

    [Fact]
    public async Task AddMariaDBContainerWithDefaultsAddsAnnotationMetadata()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder.AddMariaDB("mariadb");

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var containerResource = Assert.Single(appModel.Resources.OfType<MariaDBServerResource>());
        Assert.Equal("mariadb", containerResource.Name);

        var containerAnnotation = Assert.Single(containerResource.Annotations.OfType<ContainerImageAnnotation>());
        Assert.Equal(MariaDBContainerImageTags.Tag, containerAnnotation.Tag);
        Assert.Equal(MariaDBContainerImageTags.Image, containerAnnotation.Image);
        Assert.Equal(MariaDBContainerImageTags.Registry, containerAnnotation.Registry);

        var endpoint = Assert.Single(containerResource.Annotations.OfType<EndpointAnnotation>());
        Assert.Equal(3306, endpoint.TargetPort);
        Assert.False(endpoint.IsExternal);
        Assert.Equal("tcp", endpoint.Name);
        Assert.Null(endpoint.Port);
        Assert.Equal(ProtocolType.Tcp, endpoint.Protocol);
        Assert.Equal("tcp", endpoint.Transport);
        Assert.Equal("tcp", endpoint.UriScheme);

        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(containerResource, DistributedApplicationOperation.Run, TestServiceProvider.Instance);

        Assert.Collection(config,
            env =>
            {
                Assert.Equal("MARIADB_ROOT_PASSWORD", env.Key);
                Assert.False(string.IsNullOrEmpty(env.Value));
            });
    }

    [Fact]
    public async Task AddmariadbAddsAnnotationMetadata()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder.Configuration["Parameters:pass"] = "pass";

        var pass = appBuilder.AddParameter("pass");
        appBuilder.AddMariaDB("mariadb", pass, 1234);

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var containerResource = Assert.Single(appModel.GetContainerResources());
        Assert.Equal("mariadb", containerResource.Name);

        var containerAnnotation = Assert.Single(containerResource.Annotations.OfType<ContainerImageAnnotation>());
        Assert.Equal(MariaDBContainerImageTags.Tag, containerAnnotation.Tag);
        Assert.Equal(MariaDBContainerImageTags.Image, containerAnnotation.Image);
        Assert.Equal(MariaDBContainerImageTags.Registry, containerAnnotation.Registry);

        var endpoint = Assert.Single(containerResource.Annotations.OfType<EndpointAnnotation>());
        Assert.Equal(3306, endpoint.TargetPort);
        Assert.False(endpoint.IsExternal);
        Assert.Equal("tcp", endpoint.Name);
        Assert.Equal(1234, endpoint.Port);
        Assert.Equal(ProtocolType.Tcp, endpoint.Protocol);
        Assert.Equal("tcp", endpoint.Transport);
        Assert.Equal("tcp", endpoint.UriScheme);

        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(containerResource, DistributedApplicationOperation.Run, TestServiceProvider.Instance);

        Assert.Collection(config,
            env =>
            {
                Assert.Equal("MARIADB_ROOT_PASSWORD", env.Key);
                Assert.Equal("pass", env.Value);
            });
    }

    [Fact]
    public async Task MariaDBCreatesConnectionString()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder.AddMariaDB("mariadb")
            .WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 2000));

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var connectionStringResource = Assert.Single(appModel.Resources.OfType<IResourceWithConnectionString>());
        var connectionString = await connectionStringResource.GetConnectionStringAsync();

        Assert.Equal("Server={mariadb.bindings.tcp.host};Port={mariadb.bindings.tcp.port};User ID=root;Password={mariadb-password.value}", connectionStringResource.ConnectionStringExpression.ValueExpression);
        Assert.StartsWith("Server=localhost;Port=2000;User ID=root;Password=", connectionString);
    }

    [Fact]
    public async Task MariaDBCreatesConnectionStringWithDatabase()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder.AddMariaDB("mariadb")
            .WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 2000))
            .AddDatabase("db");

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var mariadbResource = Assert.Single(appModel.Resources.OfType<MariaDBServerResource>());
        var mariadbConnectionStringResource = (IResourceWithConnectionString)mariadbResource;
        var mariadbConnectionString = await mariadbConnectionStringResource.GetConnectionStringAsync();
        var mariadbDatabaseResource = Assert.Single(appModel.Resources.OfType<MariaDBDatabaseResource>());
        var mariadbDatabaseConnectionStringResource = (IResourceWithConnectionString)mariadbDatabaseResource;
        var dbConnectionString = await mariadbDatabaseConnectionStringResource.GetConnectionStringAsync();

        Assert.Equal(mariadbConnectionString + ";Database=db", dbConnectionString);
        Assert.Equal("{mariadb.connectionString};Database=db", mariadbDatabaseResource.ConnectionStringExpression.ValueExpression);
    }

    [Fact]
    public async Task VerifyManifest()
    {
        using var appBuilder = TestDistributedApplicationBuilder.Create();
        var mariadb = appBuilder.AddMariaDB("mariadb");
        var db = mariadb.AddDatabase("db");

        var mariadbManifest = await ManifestUtils.GetManifest(mariadb.Resource);
        var dbManifest = await ManifestUtils.GetManifest(db.Resource);

        var expectedManifest = $$"""
            {
              "type": "container.v0",
              "connectionString": "Server={mariadb.bindings.tcp.host};Port={mariadb.bindings.tcp.port};User ID=root;Password={mariadb-password.value}",
              "image": "{{MariaDBContainerImageTags.Registry}}/{{MariaDBContainerImageTags.Image}}:{{MariaDBContainerImageTags.Tag}}",
              "env": {
                "MARIADB_ROOT_PASSWORD": "{mariadb-password.value}"
              },
              "bindings": {
                "tcp": {
                  "scheme": "tcp",
                  "protocol": "tcp",
                  "transport": "tcp",
                  "targetPort": 3306
                }
              }
            }
            """;
        Assert.Equal(expectedManifest, mariadbManifest.ToString());

        expectedManifest = """
            {
              "type": "value.v0",
              "connectionString": "{mariadb.connectionString};Database=db"
            }
            """;
        Assert.Equal(expectedManifest, dbManifest.ToString());
    }

    [Fact]
    public async Task VerifyManifestWithPasswordParameter()
    {
        using var appBuilder = TestDistributedApplicationBuilder.Create();
        var pass = appBuilder.AddParameter("pass");

        var mariadb = appBuilder.AddMariaDB("mariadb", pass);
        var serverManifest = await ManifestUtils.GetManifest(mariadb.Resource);

        var expectedManifest = $$"""
            {
              "type": "container.v0",
              "connectionString": "Server={mariadb.bindings.tcp.host};Port={mariadb.bindings.tcp.port};User ID=root;Password={pass.value}",
              "image": "{{MariaDBContainerImageTags.Registry}}/{{MariaDBContainerImageTags.Image}}:{{MariaDBContainerImageTags.Tag}}",
              "env": {
                "mariadb_ROOT_PASSWORD": "{pass.value}"
              },
              "bindings": {
                "tcp": {
                  "scheme": "tcp",
                  "protocol": "tcp",
                  "transport": "tcp",
                  "targetPort": 3306
                }
              }
            }
            """;
        Assert.Equal(expectedManifest, serverManifest.ToString());
    }

    [Fact]
    public void WithAddMariaDBTwiceEndsUpWithOneAdminContainer()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.AddMariaDB("mariadb").WithPhpMyAdmin();
        builder.AddMariaDB("mariadb2").WithPhpMyAdmin();

        Assert.Single(builder.Resources.OfType<ContainerResource>().Where(resource => resource.Name is "mariadb-phpmyadmin"));
    }

    [Fact]
    public async Task SingleAddMariaDBInstanceProducesCorrectMariaDBHostsVariable()
    {
        var builder = DistributedApplication.CreateBuilder();
        var mariadb = builder.AddMariaDB("mariadb").WithPhpMyAdmin();
        using var app = builder.Build();

        // Add fake allocated endpoints.
        mariadb.WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 5001));

        await builder.Eventing.PublishAsync<AfterEndpointsAllocatedEvent>(new(app.Services, app.Services.GetRequiredService<DistributedApplicationModel>()));

        var myAdmin = builder.Resources.Single(r => r.Name.EndsWith("-phpmyadmin"));

        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(myAdmin, DistributedApplicationOperation.Run, TestServiceProvider.Instance);

        Assert.Equal($"{mariadb.Resource.Name}:{mariadb.Resource.PrimaryEndpoint.TargetPort}", config["PMA_HOST"]);
        Assert.NotNull(config["PMA_USER"]);
        Assert.NotNull(config["PMA_PASSWORD"]);
    }

    [Fact]
    public void WithPhpMyAdminAddsContainer()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.AddMariaDB("mariadb").WithPhpMyAdmin();

        var container = builder.Resources.Single(r => r.Name == "mariadb-phpmyadmin");
        var volume = container.Annotations.OfType<ContainerMountAnnotation>().Single();

        Assert.True(File.Exists(volume.Source)); // File should exist, but will be empty.
        Assert.Equal("/etc/phpmyadmin/config.user.inc.php", volume.Target);
    }

    [Fact]
    public void WithPhpMyAdminProducesValidServerConfigFile()
    {
        var builder = DistributedApplication.CreateBuilder();
        var mariadb1 = builder.AddMariaDB("mariadb1").WithPhpMyAdmin(c => c.WithHostPort(8081));
        var mariadb2 = builder.AddMariaDB("mariadb2").WithPhpMyAdmin(c => c.WithHostPort(8081));

        // Add fake allocated endpoints.
        mariadb1.WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 5001));
        mariadb2.WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 5002, "host3"));

        var myAdmin = builder.Resources.Single(r => r.Name.EndsWith("-phpmyadmin"));
        var volume = myAdmin.Annotations.OfType<ContainerMountAnnotation>().Single();

        using var app = builder.Build();
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        builder.Eventing.PublishAsync<AfterEndpointsAllocatedEvent>(new(app.Services, app.Services.GetRequiredService<DistributedApplicationModel>()));

        using var stream = File.OpenRead(volume.Source!);
        var fileContents = new StreamReader(stream).ReadToEnd();

        // check to see that the two hosts are in the file
        string pattern1 = $@"\$cfg\['Servers'\]\[\$i\]\['host'\] = '{mariadb1.Resource.Name}:{mariadb1.Resource.PrimaryEndpoint.TargetPort}';";
        string pattern2 = $@"\$cfg\['Servers'\]\[\$i\]\['host'\] = '{mariadb2.Resource.Name}:{mariadb2.Resource.PrimaryEndpoint.TargetPort}';";
        Match match1 = Regex.Match(fileContents, pattern1);
        Assert.True(match1.Success);
        Match match2 = Regex.Match(fileContents, pattern2);
        Assert.True(match2.Success);
    }

    [Fact]
    public void ThrowsWithIdenticalChildResourceNames()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var db = builder.AddMariaDB("mariadb1");
        db.AddDatabase("db");

        Assert.Throws<DistributedApplicationException>(() => db.AddDatabase("db"));
    }

    [Fact]
    public void ThrowsWithIdenticalChildResourceNamesDifferentParents()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        builder.AddMariaDB("mariadb1")
            .AddDatabase("db");

        var db = builder.AddMariaDB("mariadb2");
        Assert.Throws<DistributedApplicationException>(() => db.AddDatabase("db"));
    }

    [Fact]
    public void CanAddDatabasesWithDifferentNamesOnSingleServer()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var mariadb1 = builder.AddMariaDB("mariadb1");

        var db1 = mariadb1.AddDatabase("db1", "customers1");
        var db2 = mariadb1.AddDatabase("db2", "customers2");

        Assert.Equal(["db1", "db2"], mariadb1.Resource.Databases.Keys);
        Assert.Equal(["customers1", "customers2"], mariadb1.Resource.Databases.Values);

        Assert.Equal("customers1", db1.Resource.DatabaseName);
        Assert.Equal("customers2", db2.Resource.DatabaseName);

        Assert.Equal("{mariadb1.connectionString};Database=customers1", db1.Resource.ConnectionStringExpression.ValueExpression);
        Assert.Equal("{mariadb1.connectionString};Database=customers2", db2.Resource.ConnectionStringExpression.ValueExpression);
    }

    [Fact]
    public void CanAddDatabasesWithTheSameNameOnMultipleServers()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var db1 = builder.AddMariaDB("mariadb1")
            .AddDatabase("db1", "imports");

        var db2 = builder.AddMariaDB("mariadb2")
            .AddDatabase("db2", "imports");

        Assert.Equal("imports", db1.Resource.DatabaseName);
        Assert.Equal("imports", db2.Resource.DatabaseName);

        Assert.Equal("{mariadb1.connectionString};Database=imports", db1.Resource.ConnectionStringExpression.ValueExpression);
        Assert.Equal("{mariadb2.connectionString};Database=imports", db2.Resource.ConnectionStringExpression.ValueExpression);
    }
}
