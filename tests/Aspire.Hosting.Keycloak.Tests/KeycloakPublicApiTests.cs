// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;
using Xunit;

namespace Aspire.Hosting.Keycloak.Tests;

public class KeycloakPublicApiTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CtorKeycloakResourceShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        var name = isNull ? null! : string.Empty;
        ParameterResource? admin = null;
        var adminPassword = new ParameterResource("adminPassword", (p) => "password");

        var action = () => new KeycloakResource(name, admin, adminPassword);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }

    [Fact]
    public void CtorKeycloakResourceShouldThrowWhenAdminPasswordIsNull()
    {
        const string name = "keycloak";
        ParameterResource? admin = null;
        ParameterResource adminPassword = null!;

        var action = () => new KeycloakResource(name, admin, adminPassword);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(adminPassword), exception.ParamName);
    }

    [Fact]
    public void CtorKeycloakRealmResourceShouldThrowWhenNameIsNull()
    {
        string name = null!;
        var realmName = "realm1";
        var builder = TestDistributedApplicationBuilder.Create();
        var adminPassword = builder.AddParameter("Password");
        var parent = new KeycloakResource("keycloak", default(ParameterResource?), adminPassword.Resource);

        var action = () => new KeycloakRealmResource(name, realmName, parent);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }

    [Fact]
    public void CtorMongoKeycloakRealmResourceShouldThrowWhenRealmNameIsNull()
    {
        var name = "keycloak";
        string realmName = null!;
        var builder = TestDistributedApplicationBuilder.Create();
        var adminPassword = builder.AddParameter("Password");
        var parent = new KeycloakResource("keycloak", default(ParameterResource?), adminPassword.Resource);

        var action = () => new KeycloakRealmResource(name, realmName, parent);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(realmName), exception.ParamName);
    }

    [Fact]
    public void CtorMongoKeycloakRealmResourceShouldThrowWhenDatabaseParentIsNull()
    {
        var name = "keycloak";
        var realmName = "realm1";
        KeycloakResource parent = null!;

        var action = () => new KeycloakRealmResource(name, realmName, parent);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(parent), exception.ParamName);
    }

    [Fact]
    public void AddKeycloakShouldThrowWhenBuilderIsNull()
    {
        IDistributedApplicationBuilder builder = null!;
        const string name = "keycloak";

        var action = () => builder.AddKeycloak(name);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddKeycloakShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        var builder = TestDistributedApplicationBuilder.Create();
        var name = isNull ? null! : string.Empty;

        var action = () => builder.AddKeycloak(name);

        var exception = isNull
           ? Assert.Throws<ArgumentNullException>(action)
           : Assert.Throws<ArgumentException>(action);
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

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void WithDataBindMountShouldThrowWhenSourceIsNullOrEmpty(bool isNull)
    {
        var builder = TestDistributedApplicationBuilder.Create()
            .AddKeycloak("keycloak");
        var source = isNull ? null! : string.Empty;

        var action = () => builder.WithDataBindMount(source);

        var exception = isNull
           ? Assert.Throws<ArgumentNullException>(action)
           : Assert.Throws<ArgumentException>(action);
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

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void WithRealmImportShouldThrowWhenImportIsNullOrEmpty(bool isNull)
    {
        var builder = TestDistributedApplicationBuilder.Create()
            .AddKeycloak("keycloak");
        var import = isNull ? null! : string.Empty;

        var action = () => builder.WithRealmImport(import);

        var exception = isNull
           ? Assert.Throws<ArgumentNullException>(action)
           : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(import), exception.ParamName);
    }

    [Fact]
    public void WithRealmImportShouldThrowWhenImportDoesNotExist()
    {
        var builder = TestDistributedApplicationBuilder.Create()
            .AddKeycloak("Keycloak");

        var action = () => builder.WithRealmImport("does-not-exist");

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

    [Fact]
    public void AddRealmShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<KeycloakResource> builder = null!;
        const string name = "realm1";

        var action = () => builder.AddRealm(name);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Fact]
    public void AddRealmShouldThrowWhenNameIsNull()
    {
        var builderResource = TestDistributedApplicationBuilder.Create();
        var MongoDB = builderResource.AddKeycloak("realm1");
        string name = null!;

        var action = () => MongoDB.AddRealm(name);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }
}
