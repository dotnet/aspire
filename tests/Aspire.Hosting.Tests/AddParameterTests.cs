// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Lifecycle;
using Aspire.Hosting.Publishing;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aspire.Hosting.Tests;

public class AddParameterTests
{
    [Fact]
    public void ParametersAreHiddenByDefault()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder.Configuration["Parameters:pass"] = "pass1";

        appBuilder.AddParameter("pass", secret: true);

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var parameterResource = Assert.Single(appModel.Resources.OfType<ParameterResource>());
        var annotation = parameterResource.Annotations.OfType<ResourceSnapshotAnnotation>().SingleOrDefault();

        Assert.NotNull(annotation);

        var state = annotation.InitialSnapshot;

        Assert.Equal("Hidden", state.State);
        Assert.Collection(state.Properties,
            prop =>
            {
                Assert.Equal("parameter.secret", prop.Name);
                Assert.Equal("True", prop.Value);
            },
            prop =>
            {
                Assert.Equal(CustomResourceKnownProperties.Source, prop.Name);
                Assert.Equal("Parameters:pass", prop.Value);
            },
            prop =>
            {
                Assert.Equal("Value", prop.Name);
                Assert.Equal("pass1", prop.Value);
            });
    }

    [Fact]
    public void MissingParametersAreConfigurationMissing()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        appBuilder.AddParameter("pass");

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var parameterResource = Assert.Single(appModel.Resources.OfType<ParameterResource>());
        var annotation = parameterResource.Annotations.OfType<ResourceSnapshotAnnotation>().SingleOrDefault();

        Assert.NotNull(annotation);

        var state = annotation.InitialSnapshot;

        Assert.NotNull(state.State);
        Assert.Equal("Configuration missing", state.State.Text);
        Assert.Equal(KnownResourceStateStyles.Error, state.State.Style);
        Assert.Collection(state.Properties,
            prop =>
            {
                Assert.Equal("parameter.secret", prop.Name);
                Assert.Equal("False", prop.Value);
            },
            prop =>
            {
                Assert.Equal(CustomResourceKnownProperties.Source, prop.Name);
                Assert.Equal("Parameters:pass", prop.Value);
            },
            prop =>
            {
                Assert.Equal("Value", prop.Name);
                Assert.Contains("configuration key 'Parameters:pass' is missing", prop.Value?.ToString());
            });

        // verify that the logging hook is registered
        Assert.Contains(app.Services.GetServices<IDistributedApplicationLifecycleHook>(), hook => hook.GetType().Name == "WriteParameterLogsHook");
    }

    [Fact]
    public void ParametersWithConfigurationValueDoNotGetDefaultValue()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        appBuilder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Parameters:pass"] = "ValueFromConfiguration"
        });
        var parameter = appBuilder.AddParameter("pass");
        parameter.Resource.Default = new TestParameterDefault("DefaultValue");

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var parameterResource = Assert.Single(appModel.Resources.OfType<ParameterResource>());
        Assert.Equal("ValueFromConfiguration", parameterResource.Value);
    }

    [Fact]
    public async Task ParametersWithDefaultValueStringOverloadOnlyUsedIfNoConfigurationValue()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        appBuilder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Parameters:val1"] = "ValueFromConfiguration",
        });

        // We have 2 params, one with a config value and one without. Both get a default value.
        var parameter1 = appBuilder.AddParameter("val1", "DefaultValue1");
        var parameter2 = appBuilder.AddParameter("val2", "DefaultValue2");

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        // Make sure the config value is used for the first parameter
        var parameterResource1 = Assert.Single(appModel.Resources.OfType<ParameterResource>(), r => r.Name == "val1");
        Assert.Equal("ValueFromConfiguration", parameterResource1.Value);

        // Make sure the default value is used for the second parameter, since there is no config value
        var parameterResource2 = Assert.Single(appModel.Resources.OfType<ParameterResource>(), r => r.Name == "val2");
        Assert.Equal("DefaultValue2", parameterResource2.Value);

        // Note that the manifest should not include anything about the default value
        var param1Manifest = await ManifestUtils.GetManifest(parameter1.Resource);
        var expectedManifest = $$"""
            {
              "type": "parameter.v0",
              "value": "{val1.inputs.value}",
              "inputs": {
                "value": {
                  "type": "string"
                }
              }
            }
            """;
        Assert.Equal(expectedManifest, param1Manifest.ToString());
    }

    [Fact]
    public async Task ParametersWithDefaultValueObjectOverloadOnlyUsedIfNoConfigurationValue()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        appBuilder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Parameters:val1"] = "ValueFromConfiguration",
        });

        var genParam = new GenerateParameterDefault
        {
            MinLength = 10,
        };

        // We have 2 params, one with a config value and one without. Both get a generated param default value.
        var parameter1 = appBuilder.AddParameter("val1", genParam);
        var parameter2 = appBuilder.AddParameter("val2", genParam);

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        // Make sure the config value is used for the first parameter
        var parameterResource1 = Assert.Single(appModel.Resources.OfType<ParameterResource>(), r => r.Name == "val1");
        Assert.Equal("ValueFromConfiguration", parameterResource1.Value);

        // Make sure the generated default value is used for the second parameter, since there is no config value
        // We can't test the exact value since it's random, but we can test the length
        var parameterResource2 = Assert.Single(appModel.Resources.OfType<ParameterResource>(), r => r.Name == "val2");
        Assert.Equal(10, parameterResource2.Value.Length);

        // The manifest should include the fields for the generated default value
        var param1Manifest = await ManifestUtils.GetManifest(parameter1.Resource);
        var expectedManifest = $$"""
            {
              "type": "parameter.v0",
              "value": "{val1.inputs.value}",
              "inputs": {
                "value": {
                  "type": "string",
                  "default": {
                    "generate": {
                      "minLength": 10
                    }
                  }
                }
              }
            }
            """;
        Assert.Equal(expectedManifest, param1Manifest.ToString());
    }

    [Fact]
    public void ParametersWithDefaultValueObjectOverloadOnlyGetWrappedWhenTheyShould()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        // When using GenerateParameterDefault, it should get wrapped, since it has NeedsPersistence => true
        var parameter1 = appBuilder.AddParameter("val1", new GenerateParameterDefault());
        Assert.IsType<UserSecretsParameterDefault>(parameter1.Resource.Default);

        // When using TestParameterDefault, it should *not* get wrapped, since it has NeedsPersistence => false
        var parameter2 = appBuilder.AddParameter("val2", new TestParameterDefault("val"));
        Assert.IsType<TestParameterDefault>(parameter2.Resource.Default);
    }

    private sealed class TestParameterDefault(string defaultValue) : ParameterDefault
    {
        public override string GetDefaultValue() => defaultValue;

        public override void WriteToManifest(ManifestPublishingContext context)
        {
            throw new NotImplementedException();
        }
    }
}
