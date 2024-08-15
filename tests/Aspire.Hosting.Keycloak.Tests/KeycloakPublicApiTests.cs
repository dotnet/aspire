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
    public void WithRealmImportShouldThrowWhenImportDirectoryIsNull()
    {
        var builder = TestDistributedApplicationBuilder.Create();
        var keycloak = builder.AddKeycloak("Keycloak");
        string importDirectory = null!;

        var action = () => keycloak.WithRealmImport(importDirectory);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(importDirectory), exception.ParamName);
    }

    [Fact]
    public void WithRealmImportShouldThrowWhenImportDirectoryDoesNotExist()
    {
        var builder = TestDistributedApplicationBuilder.Create();
        var keycloak = builder.AddKeycloak("Keycloak");

        var action = () => keycloak.WithRealmImport("does-not-exist");

        Assert.Throws<DirectoryNotFoundException>(action);
    }
}
