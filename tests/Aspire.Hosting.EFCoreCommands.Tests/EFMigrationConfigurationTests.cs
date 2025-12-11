// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
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

        var options = migrations.Resource.Annotations.OfType<EFMigrationsOptions>().FirstOrDefault();
        Assert.NotNull(options);
        Assert.True(options.RunDatabaseUpdateOnStart);
    }

    [Fact]
    public void PublishAsMigrationScriptSetsOption()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var project = builder.AddProject<Projects.ServiceA>("myproject");
        var migrations = project.AddEFMigrations<TestDbContext>("mymigrations")
            .PublishAsMigrationScript();

        var options = migrations.Resource.Annotations.OfType<EFMigrationsOptions>().FirstOrDefault();
        Assert.NotNull(options);
        Assert.True(options.PublishAsMigrationScript);
    }

    [Fact]
    public void PublishAsMigrationBundleSetsOption()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var project = builder.AddProject<Projects.ServiceA>("myproject");
        var migrations = project.AddEFMigrations<TestDbContext>("mymigrations")
            .PublishAsMigrationBundle();

        var options = migrations.Resource.Annotations.OfType<EFMigrationsOptions>().FirstOrDefault();
        Assert.NotNull(options);
        Assert.True(options.PublishAsMigrationBundle);
    }

    [Fact]
    public void MultipleConfigurationOptionsCanBeChained()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var project = builder.AddProject<Projects.ServiceA>("myproject");
        var migrations = project.AddEFMigrations<TestDbContext>("mymigrations")
            .RunDatabaseUpdateOnStart()
            .PublishAsMigrationScript();

        var options = migrations.Resource.Annotations.OfType<EFMigrationsOptions>().FirstOrDefault();
        Assert.NotNull(options);
        Assert.True(options.RunDatabaseUpdateOnStart);
        Assert.True(options.PublishAsMigrationScript);
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

        var options = migrations.Resource.Annotations.OfType<EFMigrationsOptions>().FirstOrDefault();
        Assert.NotNull(options);
        Assert.True(options.RunDatabaseUpdateOnStart);
        Assert.True(options.PublishAsMigrationScript);
        Assert.True(options.PublishAsMigrationBundle);
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
        // Access via the resource since IResourceBuilder doesn't expose it
        Assert.Equal(typeof(TestDbContext).FullName, migrations.Resource.ContextTypeName);
    }

    [Fact]
    public void RunDatabaseUpdateOnStartWithNullBuilderThrows()
    {
        IResourceBuilder<EFMigrationResource>? nullBuilder = null;

        Assert.Throws<ArgumentNullException>(() =>
        {
            nullBuilder!.RunDatabaseUpdateOnStart();
        });
    }

    [Fact]
    public void PublishAsMigrationScriptWithNullBuilderThrows()
    {
        IResourceBuilder<EFMigrationResource>? nullBuilder = null;

        Assert.Throws<ArgumentNullException>(() =>
        {
            nullBuilder!.PublishAsMigrationScript();
        });
    }

    [Fact]
    public void PublishAsMigrationBundleWithNullBuilderThrows()
    {
        IResourceBuilder<EFMigrationResource>? nullBuilder = null;

        Assert.Throws<ArgumentNullException>(() =>
        {
            nullBuilder!.PublishAsMigrationBundle();
        });
    }

    [Fact]
    public void EFMigrationsOptionsOnlyCreatedOnce()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var project = builder.AddProject<Projects.ServiceA>("myproject");
        var migrations = project.AddEFMigrations<TestDbContext>("mymigrations")
            .RunDatabaseUpdateOnStart()
            .PublishAsMigrationScript()
            .PublishAsMigrationBundle();

        // Should only have one EFMigrationsOptions annotation
        var optionsCount = migrations.Resource.Annotations.OfType<EFMigrationsOptions>().Count();
        Assert.Equal(1, optionsCount);
    }

    // Test classes for DbContext types
    private sealed class TestDbContext { }
}
