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

    [Theory]
    // We test all the combinations of {direct param, callback param} x {config value, no config value}
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task ParametersWithDefaultValueStringOverloadUsedRegardlessOfConfigurationValue(bool useCallback, bool hasConfig)
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        if (hasConfig)
        {
            appBuilder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Parameters:pass"] = "ValueFromConfiguration"
            });
        }

        if (useCallback)
        {
            appBuilder.AddParameter("pass", () => "DefaultValue");
        }
        else
        {
            appBuilder.AddParameter("pass", "DefaultValue");
        }

        using var app = appBuilder.Build();
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        // Make sure the code value is used, ignoring any config value
        var parameterResource = Assert.Single(appModel.Resources.OfType<ParameterResource>(), r => r.Name == "pass");
        Assert.Equal($"DefaultValue", parameterResource.Value);

        // The manifest should not include anything about the default value
        var paramManifest = await ManifestUtils.GetManifest(appModel.Resources.OfType<ParameterResource>().Single(r => r.Name == "pass"));
        var expectedManifest = $$"""
            {
              "type": "parameter.v0",
              "value": "{pass.inputs.value}",
              "inputs": {
                "value": {
                  "type": "string"
                }
              }
            }
            """;
        Assert.Equal(expectedManifest, paramManifest.ToString());
    }

    [Theory]
    // We test all the combinations of {direct param, callback param} x {config value, no config value}
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task ParametersWithDefaultValueGetPublishedIfPublishFlagIsPassed(bool useCallback, bool hasConfig)
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        if (hasConfig)
        {
            appBuilder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Parameters:pass"] = "ValueFromConfiguration"
            });
        }

        if (useCallback)
        {
            appBuilder.AddParameter("pass", () => "DefaultValue", publishValueAsDefault: true);
        }
        else
        {
            appBuilder.AddParameter("pass", "DefaultValue", publishValueAsDefault: true);
        }

        using var app = appBuilder.Build();
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        // Make sure the code value is used, ignoring any config value
        var parameterResource = Assert.Single(appModel.Resources.OfType<ParameterResource>(), r => r.Name == "pass");
        Assert.Equal($"DefaultValue", parameterResource.Value);

        // The manifest should include the default value, since we passed publishValueAsDefault: true
        var paramManifest = await ManifestUtils.GetManifest(appModel.Resources.OfType<ParameterResource>().Single(r => r.Name == "pass"));
        var expectedManifest = $$"""
            {
              "type": "parameter.v0",
              "value": "{pass.inputs.value}",
              "inputs": {
                "value": {
                  "type": "string",
                  "default": {
                    "value": "DefaultValue"
                  }
                }
              }
            }
            """;
        Assert.Equal(expectedManifest, paramManifest.ToString());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ParametersWithDefaultValueObjectOverloadUsedRegardlessOfConfigurationValue(bool hasConfig)
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        if (hasConfig)
        {
            appBuilder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Parameters:pass"] = "ValueFromConfiguration"
            });
        }

        var genParam = new GenerateParameterDefault { MinLength = 10 };

        var parameter = appBuilder.AddParameter("pass", genParam);

        using var app = appBuilder.Build();
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        // Make sure the the generated default value is used, regardless of the config value
        // We can't test the exact value since it's random, but we can test the length
        var parameterResource = Assert.Single(appModel.Resources.OfType<ParameterResource>(), r => r.Name == "pass");
        Assert.Equal(10, parameterResource.Value.Length);

        // The manifest should include the fields for the generated default value
        var paramManifest = await ManifestUtils.GetManifest(appModel.Resources.OfType<ParameterResource>().Single(r => r.Name == "pass"));
        var expectedManifest = $$"""
            {
              "type": "parameter.v0",
              "value": "{pass.inputs.value}",
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
        Assert.Equal(expectedManifest, paramManifest.ToString());
    }

    [Fact]
    public void ParametersWithDefaultValueObjectOverloadOnlyGetWrappedWhenTheyShould()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        // Here it should get wrapped in UserSecretsParameterDefault, since we pass persist: true
        var parameter1 = appBuilder.AddParameter("val1", new GenerateParameterDefault(), persist: true);
        Assert.IsType<UserSecretsParameterDefault>(parameter1.Resource.Default);

        // Here it should not get wrapped, since we don't pass the persist flag
        var parameter2 = appBuilder.AddParameter("val2", new GenerateParameterDefault());
        Assert.IsType<GenerateParameterDefault>(parameter2.Resource.Default);
    }

    [Fact]
    public async Task ParametersCanGetValueFromNonDefaultConfigurationKeys()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        appBuilder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Parameters:val"] = "ValueFromConfigurationParams",
            ["Auth:AccessToken"] = "MyAccessToken",
        });

        var parameter = appBuilder.AddParameterFromConfiguration("val", "Auth:AccessToken");

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var parameterResource = Assert.Single(appModel.Resources.OfType<ParameterResource>(), r => r.Name == "val");
        Assert.Equal($"MyAccessToken", parameterResource.Value);

        // The manifest is not affected by the custom configuration key
        var paramManifest = await ManifestUtils.GetManifest(appModel.Resources.OfType<ParameterResource>().Single(r => r.Name == "val"));
        var expectedManifest = $$"""
                {
                  "type": "parameter.v0",
                  "value": "{val.inputs.value}",
                  "inputs": {
                    "value": {
                      "type": "string"
                    }
                  }
                }
                """;
        Assert.Equal(expectedManifest, paramManifest.ToString());
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
