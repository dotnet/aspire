// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting.EFCoreCommands.Tests;

public class EFMigrationConfigurationTests
{
    [Fact]
    public void RunDatabaseUpdateOnStartAddsAnnotation()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var project = builder.AddProject<Projects.ServiceA>("myproject");
        var migrations = project.AddEFMigrations<TestDbContext>("mymigrations")
            .RunDatabaseUpdateOnStart();

        var annotation = migrations.Resource.Annotations.OfType<RunDatabaseUpdateOnStartAnnotation>().FirstOrDefault();
        Assert.NotNull(annotation);
    }

    [Fact]
    public void PublishAsMigrationScriptAddsAnnotation()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var project = builder.AddProject<Projects.ServiceA>("myproject");
        var migrations = project.AddEFMigrations<TestDbContext>("mymigrations")
            .PublishAsMigrationScript();

        var annotation = migrations.Resource.Annotations.OfType<PublishAsMigrationScriptAnnotation>().FirstOrDefault();
        Assert.NotNull(annotation);
    }

    [Fact]
    public void PublishAsMigrationBundleAddsAnnotation()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var project = builder.AddProject<Projects.ServiceA>("myproject");
        var migrations = project.AddEFMigrations<TestDbContext>("mymigrations")
            .PublishAsMigrationBundle();

        var annotation = migrations.Resource.Annotations.OfType<PublishAsMigrationBundleAnnotation>().FirstOrDefault();
        Assert.NotNull(annotation);
    }

    [Fact]
    public void MultipleConfigurationOptionsCanBeChained()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var project = builder.AddProject<Projects.ServiceA>("myproject");
        var migrations = project.AddEFMigrations<TestDbContext>("mymigrations")
            .RunDatabaseUpdateOnStart()
            .PublishAsMigrationScript();

        Assert.NotNull(migrations.Resource.Annotations.OfType<RunDatabaseUpdateOnStartAnnotation>().FirstOrDefault());
        Assert.NotNull(migrations.Resource.Annotations.OfType<PublishAsMigrationScriptAnnotation>().FirstOrDefault());
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

    // Test classes for DbContext types
    private sealed class TestDbContext { }
}
