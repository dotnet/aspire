// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting.EntityFrameworkCore.Tests;

public class EFMigrationConfigurationTests
{
    [Fact]
    public void RunDatabaseUpdateOnStartSetsOption()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var project = builder.AddProject<Projects.ServiceA>("myproject");
        var migrations = project.AddEFMigrations<TestDbContext>("mymigrations")
            .RunDatabaseUpdateOnStart();

        Assert.True(migrations.Resource.RunDatabaseUpdateOnStart);
    }

    [Fact]
    public void RunDatabaseUpdateOnStartRegistersHealthCheck()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var project = builder.AddProject<Projects.ServiceA>("myproject");
        var migrations = project.AddEFMigrations<TestDbContext>("mymigrations")
            .RunDatabaseUpdateOnStart();

        // A health check annotation should be added
        Assert.True(migrations.Resource.TryGetAnnotationsOfType<HealthCheckAnnotation>(out var annotations));
        Assert.Single(annotations);
        Assert.Contains("migration_healthcheck", annotations.First().Key);
    }

    [Fact]
    public void PublishAsMigrationScriptSetsOption()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var project = builder.AddProject<Projects.ServiceA>("myproject");
        var migrations = project.AddEFMigrations<TestDbContext>("mymigrations")
            .PublishAsMigrationScript();

        Assert.True(migrations.Resource.PublishAsMigrationScript);
    }

    [Fact]
    public void PublishAsMigrationBundleSetsOption()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var project = builder.AddProject<Projects.ServiceA>("myproject");
        var migrations = project.AddEFMigrations<TestDbContext>("mymigrations")
            .PublishAsMigrationBundle();

        Assert.True(migrations.Resource.PublishAsMigrationBundle);
    }

    [Fact]
    public void WithMigrationOutputDirectorySetsOption()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var project = builder.AddProject<Projects.ServiceA>("myproject");
        var migrations = project.AddEFMigrations<TestDbContext>("mymigrations")
            .WithMigrationOutputDirectory("Data/Migrations");

        Assert.Equal("Data/Migrations", migrations.Resource.MigrationOutputDirectory);
    }

    [Fact]
    public void WithMigrationNamespaceSetsOption()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var project = builder.AddProject<Projects.ServiceA>("myproject");
        var migrations = project.AddEFMigrations<TestDbContext>("mymigrations")
            .WithMigrationNamespace("MyApp.Data.Migrations");

        Assert.Equal("MyApp.Data.Migrations", migrations.Resource.MigrationNamespace);
    }

    [Fact]
    public void WithMigrationOutputDirectoryThrowsForEmptyString()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var project = builder.AddProject<Projects.ServiceA>("myproject");
        var migrations = project.AddEFMigrations<TestDbContext>("mymigrations");

        Assert.Throws<ArgumentException>(() => migrations.WithMigrationOutputDirectory(""));
    }

    [Fact]
    public void WithMigrationNamespaceThrowsForEmptyString()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var project = builder.AddProject<Projects.ServiceA>("myproject");
        var migrations = project.AddEFMigrations<TestDbContext>("mymigrations");

        Assert.Throws<ArgumentException>(() => migrations.WithMigrationNamespace(""));
    }

    [Fact]
    public void MultipleConfigurationOptionsCanBeChained()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var project = builder.AddProject<Projects.ServiceA>("myproject");
        var migrations = project.AddEFMigrations<TestDbContext>("mymigrations")
            .RunDatabaseUpdateOnStart()
            .PublishAsMigrationScript();

        Assert.True(migrations.Resource.RunDatabaseUpdateOnStart);
        Assert.True(migrations.Resource.PublishAsMigrationScript);
    }

    [Fact]
    public void AllConfigurationOptionsCanBeChained()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var project = builder.AddProject<Projects.ServiceA>("myproject");
        var migrations = project.AddEFMigrations<TestDbContext>("mymigrations")
            .RunDatabaseUpdateOnStart()
            .PublishAsMigrationScript()
            .PublishAsMigrationBundle()
            .WithMigrationOutputDirectory("CustomDir")
            .WithMigrationNamespace("MyApp.Migrations");

        Assert.True(migrations.Resource.RunDatabaseUpdateOnStart);
        Assert.True(migrations.Resource.PublishAsMigrationScript);
        Assert.True(migrations.Resource.PublishAsMigrationBundle);
        Assert.Equal("CustomDir", migrations.Resource.MigrationOutputDirectory);
        Assert.Equal("MyApp.Migrations", migrations.Resource.MigrationNamespace);
    }

    [Fact]
    public void ConfigurationMethodsPreserveContextTypeName()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var project = builder.AddProject<Projects.ServiceA>("myproject");
        var migrations = project.AddEFMigrations<TestDbContext>("mymigrations")
            .RunDatabaseUpdateOnStart()
            .PublishAsMigrationScript()
            .PublishAsMigrationBundle();

        // The context type name should be preserved through chaining
        Assert.Equal(typeof(TestDbContext).FullName, migrations.Resource.ContextTypeName);
    }

    [Fact]
    public void ConfigurationMethodsReturnSameBuilderType()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var project = builder.AddProject<Projects.ServiceA>("myproject");
        var migrations = project.AddEFMigrations<TestDbContext>("mymigrations");
        
        var afterUpdate = migrations.RunDatabaseUpdateOnStart();
        var afterScript = afterUpdate.PublishAsMigrationScript();
        var afterBundle = afterScript.PublishAsMigrationBundle();
        var afterOutputDir = afterBundle.WithMigrationOutputDirectory("Migrations");
        var afterNamespace = afterOutputDir.WithMigrationNamespace("MyApp.Migrations");

        // All methods should return EFMigrationResourceBuilder for proper chaining
        Assert.IsType<EFMigrationResourceBuilder>(afterUpdate);
        Assert.IsType<EFMigrationResourceBuilder>(afterScript);
        Assert.IsType<EFMigrationResourceBuilder>(afterBundle);
        Assert.IsType<EFMigrationResourceBuilder>(afterOutputDir);
        Assert.IsType<EFMigrationResourceBuilder>(afterNamespace);
    }

    [Fact]
    public void OptionsInitiallyFalseOrNull()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var project = builder.AddProject<Projects.ServiceA>("myproject");
        var migrations = project.AddEFMigrations<TestDbContext>("mymigrations");

        // Options should all be false/null initially
        Assert.False(migrations.Resource.RunDatabaseUpdateOnStart);
        Assert.False(migrations.Resource.PublishAsMigrationScript);
        Assert.False(migrations.Resource.PublishAsMigrationBundle);
        Assert.Null(migrations.Resource.MigrationOutputDirectory);
        Assert.Null(migrations.Resource.MigrationNamespace);
        Assert.Null(migrations.Resource.MigrationsProject);
    }

    [Fact]
    public void WithMigrationsProjectSetsOption()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var startupProject = builder.AddProject<Projects.ServiceA>("startup");
        var targetProject = builder.AddProject<Projects.ServiceB>("target");
        var migrations = startupProject.AddEFMigrations<TestDbContext>("mymigrations")
            .WithMigrationsProject(targetProject);

        Assert.Equal(targetProject.Resource, migrations.Resource.MigrationsProject);
    }

    [Fact]
    public void WithMigrationsProjectThrowsForNull()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var project = builder.AddProject<Projects.ServiceA>("myproject");
        var migrations = project.AddEFMigrations<TestDbContext>("mymigrations");

        Assert.Throws<ArgumentNullException>(() => migrations.WithMigrationsProject(null!));
    }

    // Test classes for DbContext types
    private sealed class TestDbContext { }
}
