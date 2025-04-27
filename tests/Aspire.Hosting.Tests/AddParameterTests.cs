// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Publishing;
using Aspire.Hosting.Utils;
using Microsoft.AspNetCore.InternalTesting;
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
            });
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
        var paramManifest = await ManifestUtils.GetManifest(appModel.Resources.OfType<ParameterResource>().Single(r => r.Name == "pass")).DefaultTimeout();
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
        var paramManifest = await ManifestUtils.GetManifest(appModel.Resources.OfType<ParameterResource>().Single(r => r.Name == "pass")).DefaultTimeout();
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

    [Fact]
    public void AddParameterWithBothPublishValueAsDefaultAndSecretFails()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        // publishValueAsDefault and secret are mutually exclusive. Test both overloads.
        var ex1 = Assert.Throws<ArgumentException>(() => appBuilder.AddParameter("pass", () => "SomeSecret", publishValueAsDefault: true, secret: true));
        Assert.Equal($"A parameter cannot be both secret and published as a default value. (Parameter 'secret')", ex1.Message);
        var ex2 = Assert.Throws<ArgumentException>(() => appBuilder.AddParameter("pass", "SomeSecret", publishValueAsDefault: true, secret: true));
        Assert.Equal($"A parameter cannot be both secret and published as a default value. (Parameter 'secret')", ex2.Message);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ParametersWithDefaultValueObjectOverloadUseConfigurationValueWhenPresent(bool hasConfig)
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

        // Make sure the the generated default value is only used when there isn't a config value
        var parameterResource = Assert.Single(appModel.Resources.OfType<ParameterResource>(), r => r.Name == "pass");
        if (hasConfig)
        {
            Assert.Equal("ValueFromConfiguration", parameterResource.Value);
        }
        else
        {
            Assert.NotEqual("ValueFromConfiguration", parameterResource.Value);
            // We can't test the exact value since it's random, but we can test the length
            Assert.Equal(10, parameterResource.Value.Length);
        }

        // The manifest should always include the fields for the generated default value
        var paramManifest = await ManifestUtils.GetManifest(appModel.Resources.OfType<ParameterResource>().Single(r => r.Name == "pass")).DefaultTimeout();
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
        var paramManifest = await ManifestUtils.GetManifest(appModel.Resources.OfType<ParameterResource>().Single(r => r.Name == "val")).DefaultTimeout();
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

    [Fact]
    public async Task AddConnectionStringParameterIsASecretParameterInTheManifest()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        appBuilder.AddConnectionString("mycs");

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var connectionStringResource = Assert.Single(appModel.Resources.OfType<ParameterResource>());

        Assert.Equal("mycs", connectionStringResource.Name);
        var connectionStringManifest = await ManifestUtils.GetManifest(connectionStringResource).DefaultTimeout();

        var expectedManifest = $$"""
            {
              "type": "parameter.v0",
              "connectionString": "{mycs.value}",
              "value": "{mycs.inputs.value}",
              "inputs": {
                "value": {
                  "type": "string",
                  "secret": true
                }
              }
            }
            """;

        var s = connectionStringManifest.ToString();

        Assert.Equal(expectedManifest, s);
    }

    [Fact]
    public async Task AddConnectionStringExpressionIsAValueInTheManifest()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        var endpoint = appBuilder.AddParameter("endpoint", "http://localhost:3452");
        var key = appBuilder.AddParameter("key", "secretKey", secret: true);

        // Get the service provider.
        appBuilder.AddConnectionString("mycs", ReferenceExpression.Interpolate($"Endpoint={endpoint};Key={key}"));

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var connectionStringResource = Assert.Single(appModel.Resources.OfType<ConnectionStringResource>());

        Assert.Equal("mycs", connectionStringResource.Name);
        var connectionStringManifest = await ManifestUtils.GetManifest(connectionStringResource).DefaultTimeout();

        var expectedManifest = $$"""
            {
              "type": "value.v0",
              "connectionString": "Endpoint={endpoint.value};Key={key.value}"
            }
            """;

        var s = connectionStringManifest.ToString();

        Assert.Equal(expectedManifest, s);
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
