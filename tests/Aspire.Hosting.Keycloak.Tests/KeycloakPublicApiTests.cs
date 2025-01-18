// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;
using Xunit;

namespace Aspire.Hosting.Keycloak.Tests;

public class KeycloakPublicApiTests
{
    [Fact]
    public void CtorKeycloakResourceShouldThrowWhenNameIsNull()
    {
        string name = null!;
        var builder = TestDistributedApplicationBuilder.Create();
        var adminPassword = builder.AddParameter("Password");

        var action = () => new KeycloakResource(name, default(ParameterResource?), adminPassword.Resource);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }

    [Fact]
    public void CtorKeycloakResourceShouldThrowWhenAdminPasswordIsNull()
    {
        const string name = "Keycloak";
        ParameterResource adminPassword = null!;

        var action = () => new KeycloakResource(name, default(ParameterResource?), adminPassword);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(adminPassword), exception.ParamName);
    }

    [Fact]
    public void AddKeycloakContainerShouldThrowWhenBuilderIsNull()
    {
        IDistributedApplicationBuilder builder =  null!;
        const string name = "Keycloak";

        var action = () => builder.AddKeycloak(name);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Fact]
    public void AddKeycloakContainerShouldThrowWhenNameIsNull()
    {
        var builder = TestDistributedApplicationBuilder.Create();
        string name = null!;

        var action = () => builder.AddKeycloak(name);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }

    [Fact]
    public void WithDataVolumeShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<KeycloakResource> builder = null!;

        var action = () => builder.WithDataVolume();

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Fact]
    public void WithDataBindMountShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<KeycloakResource> builder = null!;
        const string source = "/opt/keycloak/data";

        var action = () => builder.WithDataBindMount(source);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Fact]
    public void WithDataBindMountShouldThrowWhenSourceIsNull()
    {
        var builder = TestDistributedApplicationBuilder.Create();
        var keycloak = builder.AddKeycloak("Keycloak");
        string source = null!;

        var action = () => keycloak.WithDataBindMount(source);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(source), exception.ParamName);
    }

    [Fact]
    public void WithRealmImportShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<KeycloakResource> builder = null!;
        const string importDirectory = "/opt/keycloak/data/import";

        var action = () => builder.WithRealmImport(importDirectory);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Fact]
    public void WithRealmImportShouldThrowWhenImportIsNull()
    {
        var builder = TestDistributedApplicationBuilder.Create();
        var keycloak = builder.AddKeycloak("Keycloak");
        string import = null!;

        var action = () => keycloak.WithRealmImport(import);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(import), exception.ParamName);
    }

    [Fact]
    public void WithRealmImportShouldThrowWhenImportDoesNotExist()
    {
        var builder = TestDistributedApplicationBuilder.Create();
        var keycloak = builder.AddKeycloak("Keycloak");

        var action = () => keycloak.WithRealmImport("does-not-exist");

        Assert.Throws<InvalidOperationException>(action);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(true)]
    [InlineData(false)]
    public void WithRealmImportDirectoryAddsBindMountAnnotation(bool? isReadOnly)
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDirectory);

        var resourceName = "keycloak";
        var keycloak = builder.AddKeycloak(resourceName);

        if (isReadOnly.HasValue)
        {
            keycloak.WithRealmImport(tempDirectory, isReadOnly: isReadOnly.Value);
        }
        else
        {
            keycloak.WithRealmImport(tempDirectory);
        }

        var containerAnnotation = keycloak.Resource.Annotations.OfType<ContainerMountAnnotation>().Single();

        Assert.Equal(tempDirectory, containerAnnotation.Source);
        Assert.Equal("/opt/keycloak/data/import", containerAnnotation.Target);
        Assert.Equal(ContainerMountType.BindMount, containerAnnotation.Type);
        Assert.Equal(isReadOnly ?? false, containerAnnotation.IsReadOnly);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(true)]
    [InlineData(false)]
    public void WithRealmImportFileAddsBindMountAnnotation(bool? isReadOnly)
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDirectory);

        var file = "realm.json";
        var filePath = Path.Combine(tempDirectory, file);
        File.WriteAllText(filePath, string.Empty);

        var resourceName = "keycloak";
        var keycloak = builder.AddKeycloak(resourceName);

        if (isReadOnly.HasValue)
        {
            keycloak.WithRealmImport(filePath, isReadOnly: isReadOnly.Value);
        }
        else
        {
            keycloak.WithRealmImport(filePath);
        }

        var containerAnnotation = keycloak.Resource.Annotations.OfType<ContainerMountAnnotation>().Single();

        Assert.Equal(filePath, containerAnnotation.Source);
        Assert.Equal($"/opt/keycloak/data/import/{file}", containerAnnotation.Target);
        Assert.Equal(ContainerMountType.BindMount, containerAnnotation.Type);
        Assert.Equal(isReadOnly ?? false, containerAnnotation.IsReadOnly);
    }
}
