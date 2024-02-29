// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using Aspire.Hosting.MySql;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting.Tests.MySql;

public class AddMySqlTests
{
    [Fact]
    public void AddMySqlContainerWithDefaultsAddsAnnotationMetadata()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder.AddMySql("mysql");

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var containerResource = Assert.Single(appModel.Resources.OfType<MySqlServerResource>());
        Assert.Equal("mysql", containerResource.Name);

        var manifestAnnotation = Assert.Single(containerResource.Annotations.OfType<ManifestPublishingCallbackAnnotation>());
        Assert.NotNull(manifestAnnotation.Callback);

        var containerAnnotation = Assert.Single(containerResource.Annotations.OfType<ContainerImageAnnotation>());
        Assert.Equal("8.3.0", containerAnnotation.Tag);
        Assert.Equal("mysql", containerAnnotation.Image);
        Assert.Null(containerAnnotation.Registry);

        var endpoint = Assert.Single(containerResource.Annotations.OfType<EndpointAnnotation>());
        Assert.Equal(3306, endpoint.ContainerPort);
        Assert.False(endpoint.IsExternal);
        Assert.Equal("tcp", endpoint.Name);
        Assert.Null(endpoint.Port);
        Assert.Equal(ProtocolType.Tcp, endpoint.Protocol);
        Assert.Equal("tcp", endpoint.Transport);
        Assert.Equal("tcp", endpoint.UriScheme);

        var envAnnotations = containerResource.Annotations.OfType<EnvironmentCallbackAnnotation>();

        var config = new Dictionary<string, string>();
        var executionContext = new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run);
        var context = new EnvironmentCallbackContext(executionContext, config);

        foreach (var annotation in envAnnotations)
        {
            annotation.Callback(context);
        }

        Assert.Collection(config,
            env =>
            {
                Assert.Equal("MYSQL_ROOT_PASSWORD", env.Key);
                Assert.False(string.IsNullOrEmpty(env.Value));
            });
    }

    [Fact]
    public void AddMySqlAddsAnnotationMetadata()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder.AddMySql("mysql", 1234, "pass");

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var containerResource = Assert.Single(appModel.GetContainerResources());
        Assert.Equal("mysql", containerResource.Name);

        var manifestPublishing = Assert.Single(containerResource.Annotations.OfType<ManifestPublishingCallbackAnnotation>());
        Assert.NotNull(manifestPublishing.Callback);

        var containerAnnotation = Assert.Single(containerResource.Annotations.OfType<ContainerImageAnnotation>());
        Assert.Equal("8.3.0", containerAnnotation.Tag);
        Assert.Equal("mysql", containerAnnotation.Image);
        Assert.Null(containerAnnotation.Registry);

        var endpoint = Assert.Single(containerResource.Annotations.OfType<EndpointAnnotation>());
        Assert.Equal(3306, endpoint.ContainerPort);
        Assert.False(endpoint.IsExternal);
        Assert.Equal("tcp", endpoint.Name);
        Assert.Equal(1234, endpoint.Port);
        Assert.Equal(ProtocolType.Tcp, endpoint.Protocol);
        Assert.Equal("tcp", endpoint.Transport);
        Assert.Equal("tcp", endpoint.UriScheme);

        var envAnnotations = containerResource.Annotations.OfType<EnvironmentCallbackAnnotation>();

        var config = new Dictionary<string, string>();
        var executionContext = new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run);
        var context = new EnvironmentCallbackContext(executionContext, config);

        foreach (var annotation in envAnnotations)
        {
            annotation.Callback(context);
        }

        Assert.Collection(config,
            env =>
            {
                Assert.Equal("MYSQL_ROOT_PASSWORD", env.Key);
                Assert.Equal("pass", env.Value);
            });
    }

    [Fact]
    public void MySqlCreatesConnectionString()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder.AddMySql("mysql")
            .WithAnnotation(
            new AllocatedEndpointAnnotation("mybinding",
            ProtocolType.Tcp,
            "localhost",
            2000,
            "https"
            ));

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var connectionStringResource = Assert.Single(appModel.Resources.OfType<IResourceWithConnectionString>());
        var connectionString = connectionStringResource.GetConnectionString();

        Assert.Equal("Server={mysql.bindings.tcp.host};Port={mysql.bindings.tcp.port};User ID=root;Password={mysql.inputs.password}", connectionStringResource.ConnectionStringExpression);
        Assert.StartsWith("Server=localhost;Port=2000;User ID=root;Password=", connectionString);
    }

    [Fact]
    public void MySqlCreatesConnectionStringWithDatabase()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder.AddMySql("mysql")
            .WithAnnotation(
            new AllocatedEndpointAnnotation("mybinding",
            ProtocolType.Tcp,
            "localhost",
            2000,
            "https"
            ))
            .AddDatabase("db");

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var mySqlResource = Assert.Single(appModel.Resources.OfType<MySqlServerResource>());
        var mySqlConnectionString = mySqlResource.GetConnectionString();
        var mySqlDatabaseResource = Assert.Single(appModel.Resources.OfType<MySqlDatabaseResource>());
        var dbConnectionString = mySqlDatabaseResource.GetConnectionString();

        Assert.Equal(mySqlConnectionString + ";Database=db", dbConnectionString);
        Assert.Equal("{mysql.connectionString};Database=db", mySqlDatabaseResource.ConnectionStringExpression);
    }

    [Fact]
    public void VerifyManifest()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        var mysql = appBuilder.AddMySql("mysql");
        var db = mysql.AddDatabase("db");

        var mySqlManifest = ManifestUtils.GetManifest(mysql.Resource);
        var dbManifest = ManifestUtils.GetManifest(db.Resource);

        Assert.Equal("container.v0", mySqlManifest["type"]?.ToString());
        Assert.Equal(mysql.Resource.ConnectionStringExpression, mySqlManifest["connectionString"]?.ToString());

        Assert.Equal("value.v0", dbManifest["type"]?.ToString());
        Assert.Equal(db.Resource.ConnectionStringExpression, dbManifest["connectionString"]?.ToString());
    }

    [Fact]
    public void WithMySqlTwiceEndsUpWithOneAdminContainer()
    {
        var builder = DistributedApplication.CreateBuilder();
        builder.AddMySql("mySql").WithPhpMyAdmin();
        builder.AddMySql("mySql2").WithPhpMyAdmin();

        Assert.Single(builder.Resources.OfType<PhpMyAdminContainerResource>());
    }

    [Fact]
    public async Task SingleMySqlInstanceProducesCorrectMySqlHostsVariable()
    {
        var builder = DistributedApplication.CreateBuilder();
        var mysql = builder.AddMySql("mySql").WithPhpMyAdmin();
        using var app = builder.Build();

        // Add fake allocated endpoints.
        mysql.WithAnnotation(new AllocatedEndpointAnnotation("tcp", ProtocolType.Tcp, "host.docker.internal", 5001, "tcp"));

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();
        var hook = new PhpMyAdminConfigWriterHook();
        await hook.AfterEndpointsAllocatedAsync(model, CancellationToken.None);

        var myAdmin = builder.Resources.Single(r => r.Name.EndsWith("-phpmyadmin"));

        var envAnnotations = myAdmin.Annotations.OfType<EnvironmentCallbackAnnotation>();

        var config = new Dictionary<string, string>();
        var executionContext = new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run);
        var context = new EnvironmentCallbackContext(executionContext, config);

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
        var volume = container.Annotations.OfType<ContainerMountAnnotation>().Single();

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
        var volume = myAdmin.Annotations.OfType<ContainerMountAnnotation>().Single();

        using var app = builder.Build();
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var hook = new PhpMyAdminConfigWriterHook();
        hook.AfterEndpointsAllocatedAsync(appModel, CancellationToken.None);

        using var stream = File.OpenRead(volume.Source!);
        var fileContents = new StreamReader(stream).ReadToEnd();

        // check to see that the two hosts are in the file
        string pattern1 = @"\$cfg\['Servers'\]\[\$i\]\['host'\] = 'host.docker.internal:5001';";
        string pattern2 = @"\$cfg\['Servers'\]\[\$i\]\['host'\] = 'host.docker.internal:5002';";
        Match match1 = Regex.Match(fileContents, pattern1);
        Assert.True(match1.Success);
        Match match2 = Regex.Match(fileContents, pattern2);
        Assert.True(match2.Success);
    }

    [Fact]
    public void ThrowsWithIdenticalChildResourceNames()
    {
        var builder = DistributedApplication.CreateBuilder();

        var db = builder.AddMySql("mysql1");
        db.AddDatabase("db");

        Assert.Throws<DistributedApplicationException>(() => db.AddDatabase("db"));
    }

    [Fact]
    public void ThrowsWithIdenticalChildResourceNamesDifferentParents()
    {
        var builder = DistributedApplication.CreateBuilder();

        builder.AddMySql("mysql1")
            .AddDatabase("db");

        var db = builder.AddMySql("mysql2");
        Assert.Throws<DistributedApplicationException>(() => db.AddDatabase("db"));
    }

    [Fact]
    public void CanAddDatabasesWithDifferentNamesOnSingleServer()
    {
        var builder = DistributedApplication.CreateBuilder();

        var mysql1 = builder.AddMySql("mysql1");

        var db1 = mysql1.AddDatabase("db1", "customers1");
        var db2 = mysql1.AddDatabase("db2", "customers2");

        Assert.Equal(["db1", "db2"], mysql1.Resource.Databases.Keys);
        Assert.Equal(["customers1", "customers2"], mysql1.Resource.Databases.Values);

        Assert.Equal("customers1", db1.Resource.DatabaseName);
        Assert.Equal("customers2", db2.Resource.DatabaseName);

        Assert.Equal("{mysql1.connectionString};Database=customers1", db1.Resource.ConnectionStringExpression);
        Assert.Equal("{mysql1.connectionString};Database=customers2", db2.Resource.ConnectionStringExpression);
    }

    [Fact]
    public void CanAddDatabasesWithTheSameNameOnMultipleServers()
    {
        var builder = DistributedApplication.CreateBuilder();

        var db1 = builder.AddMySql("mysql1")
            .AddDatabase("db1", "imports");

        var db2 = builder.AddMySql("mysql2")
            .AddDatabase("db2", "imports");

        Assert.Equal("imports", db1.Resource.DatabaseName);
        Assert.Equal("imports", db2.Resource.DatabaseName);

        Assert.Equal("{mysql1.connectionString};Database=imports", db1.Resource.ConnectionStringExpression);
        Assert.Equal("{mysql2.connectionString};Database=imports", db2.Resource.ConnectionStringExpression);
    }
}
