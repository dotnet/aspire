// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting.EFCoreCommands.Tests;

public class AddEFMigrationsTests
{
    [Fact]
    public void AddEFMigrationsCreatesResource()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var project = builder.AddProject<Projects.ServiceA>("myproject");
        var migrations = project.AddEFMigrations<TestDbContext>("mymigrations");

        Assert.NotNull(migrations);
        Assert.Equal("mymigrations", migrations.Resource.Name);
        Assert.Equal(project.Resource, migrations.Resource.ProjectResource);
        Assert.Equal(typeof(TestDbContext), migrations.Resource.ContextType);
    }

    [Fact]
    public void AddEFMigrationsWithoutContextTypeCreatesResource()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var project = builder.AddProject<Projects.ServiceA>("myproject");
        var migrations = project.AddEFMigrations("mymigrations");

        Assert.NotNull(migrations);
        Assert.Equal("mymigrations", migrations.Resource.Name);
        Assert.Equal(project.Resource, migrations.Resource.ProjectResource);
        Assert.Null(migrations.Resource.ContextType);
    }

    [Fact]
    public void AddEFMigrationsWithExplicitContextTypeCreatesResource()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var project = builder.AddProject<Projects.ServiceA>("myproject");
        var migrations = project.AddEFMigrations("mymigrations", typeof(TestDbContext));

        Assert.NotNull(migrations);
        Assert.Equal("mymigrations", migrations.Resource.Name);
        Assert.Equal(project.Resource, migrations.Resource.ProjectResource);
        Assert.Equal(typeof(TestDbContext), migrations.Resource.ContextType);
    }

    [Fact]
    public void AddEFMigrationsAddsResourceToAppModel()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var project = builder.AddProject<Projects.ServiceA>("myproject");
        project.AddEFMigrations<TestDbContext>("mymigrations");

        using var app = builder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var migrationResource = Assert.Single(appModel.Resources.OfType<EFMigrationResource>());
        Assert.Equal("mymigrations", migrationResource.Name);
    }

    [Fact]
    public void AddEFMigrationsForMultipleContextsSucceeds()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var project = builder.AddProject<Projects.ServiceA>("myproject");

        var migrations1 = project.AddEFMigrations<TestDbContext>("migrations1");
        var migrations2 = project.AddEFMigrations<AnotherDbContext>("migrations2");

        Assert.NotEqual(migrations1.Resource, migrations2.Resource);
        Assert.Equal(typeof(TestDbContext), migrations1.Resource.ContextType);
        Assert.Equal(typeof(AnotherDbContext), migrations2.Resource.ContextType);
    }

    [Fact]
    public void AddEFMigrationsDuplicateContextTypeThrows()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var project = builder.AddProject<Projects.ServiceA>("myproject");

        project.AddEFMigrations<TestDbContext>("migrations1");

        var exception = Assert.Throws<InvalidOperationException>(() =>
        {
            project.AddEFMigrations<TestDbContext>("migrations2");
        });

        Assert.Contains("TestDbContext", exception.Message);
        Assert.Contains("already been added", exception.Message);
    }

    [Fact]
    public void AddEFMigrationsWithNullNameThrows()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var project = builder.AddProject<Projects.ServiceA>("myproject");

        Assert.Throws<ArgumentNullException>(() =>
        {
            project.AddEFMigrations<TestDbContext>(null!);
        });
    }

    [Fact]
    public void AddEFMigrationsWithEmptyNameThrows()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var project = builder.AddProject<Projects.ServiceA>("myproject");

        Assert.Throws<ArgumentException>(() =>
        {
            project.AddEFMigrations<TestDbContext>("");
        });
    }

    [Fact]
    public void AddEFMigrationsHasResourceSnapshotAnnotation()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var project = builder.AddProject<Projects.ServiceA>("myproject");
        var migrations = project.AddEFMigrations<TestDbContext>("mymigrations");

        var snapshotAnnotation = migrations.Resource.Annotations.OfType<ResourceSnapshotAnnotation>().FirstOrDefault();
        Assert.NotNull(snapshotAnnotation);
        Assert.Equal("EFMigration", snapshotAnnotation.InitialSnapshot.ResourceType);
        Assert.Equal("Pending", snapshotAnnotation.InitialSnapshot.State?.Text);
    }

    [Fact]
    public void AddEFMigrationsHasDatabaseIcon()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var project = builder.AddProject<Projects.ServiceA>("myproject");
        var migrations = project.AddEFMigrations<TestDbContext>("mymigrations");

        var iconAnnotation = migrations.Resource.Annotations.OfType<ResourceIconAnnotation>().FirstOrDefault();
        Assert.NotNull(iconAnnotation);
        Assert.Equal("Database", iconAnnotation.IconName);
    }

    [Fact]
    public void EFMigrationResourceImplementsIResourceWithWaitSupport()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var project = builder.AddProject<Projects.ServiceA>("myproject");
        var migrations = project.AddEFMigrations<TestDbContext>("mymigrations");

        Assert.IsAssignableFrom<IResourceWithWaitSupport>(migrations.Resource);
    }

    // Test classes for DbContext types
    private sealed class TestDbContext { }
    private sealed class AnotherDbContext { }
}
