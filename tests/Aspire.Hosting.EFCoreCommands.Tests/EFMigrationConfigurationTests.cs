// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Utils;

namespace Aspire.Hosting.EFCoreCommands.Tests;

public class EFMigrationConfigurationTests
{
    [Fact]
    public void RunDatabaseUpdateOnStartSetsOption()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var project = builder.AddProject<Projects.ServiceA>("myproject");
        var migrations = project.AddEFMigrations<TestDbContext>("mymigrations")
            .RunDatabaseUpdateOnStart();

        Assert.True(migrations.Resource.Options.RunDatabaseUpdateOnStart);
    }

    [Fact]
    public void PublishAsMigrationScriptSetsOption()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var project = builder.AddProject<Projects.ServiceA>("myproject");
        var migrations = project.AddEFMigrations<TestDbContext>("mymigrations")
            .PublishAsMigrationScript();

        Assert.True(migrations.Resource.Options.PublishAsMigrationScript);
    }

    [Fact]
    public void PublishAsMigrationBundleSetsOption()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var project = builder.AddProject<Projects.ServiceA>("myproject");
        var migrations = project.AddEFMigrations<TestDbContext>("mymigrations")
            .PublishAsMigrationBundle();

        Assert.True(migrations.Resource.Options.PublishAsMigrationBundle);
    }

    [Fact]
    public void MultipleConfigurationOptionsCanBeChained()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var project = builder.AddProject<Projects.ServiceA>("myproject");
        var migrations = project.AddEFMigrations<TestDbContext>("mymigrations")
            .RunDatabaseUpdateOnStart()
            .PublishAsMigrationScript();

        Assert.True(migrations.Resource.Options.RunDatabaseUpdateOnStart);
        Assert.True(migrations.Resource.Options.PublishAsMigrationScript);
    }

    [Fact]
    public void AllConfigurationOptionsCanBeChained()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var project = builder.AddProject<Projects.ServiceA>("myproject");
        var migrations = project.AddEFMigrations<TestDbContext>("mymigrations")
            .RunDatabaseUpdateOnStart()
            .PublishAsMigrationScript()
            .PublishAsMigrationBundle();

        Assert.True(migrations.Resource.Options.RunDatabaseUpdateOnStart);
        Assert.True(migrations.Resource.Options.PublishAsMigrationScript);
        Assert.True(migrations.Resource.Options.PublishAsMigrationBundle);
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
        Assert.Equal(typeof(TestDbContext).FullName, migrations.ContextTypeName);
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

        // All methods should return EFMigrationResourceBuilder for proper chaining
        Assert.IsType<EFMigrationResourceBuilder>(afterUpdate);
        Assert.IsType<EFMigrationResourceBuilder>(afterScript);
        Assert.IsType<EFMigrationResourceBuilder>(afterBundle);
    }

    [Fact]
    public void OptionsInitiallyFalse()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var project = builder.AddProject<Projects.ServiceA>("myproject");
        var migrations = project.AddEFMigrations<TestDbContext>("mymigrations");

        // Options should all be false initially
        Assert.False(migrations.Resource.Options.RunDatabaseUpdateOnStart);
        Assert.False(migrations.Resource.Options.PublishAsMigrationScript);
        Assert.False(migrations.Resource.Options.PublishAsMigrationBundle);
    }

    // Test classes for DbContext types
    private sealed class TestDbContext { }
}
