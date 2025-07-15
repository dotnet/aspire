// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Aspire.Hosting.Publishing;
using Aspire.Hosting.Utils;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting.Tests;

public class AddParameterTests
{
    [Fact]
    public void ParametersAreVisibleByDefault()
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

        Assert.False(state.IsHidden);
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
        appBuilder.AddConnectionString("mycs", ReferenceExpression.Create($"Endpoint={endpoint};Key={key}"));

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

    [Fact]
    public void ConnectionStringsAreVisibleByDefault()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        var endpoint = appBuilder.AddParameter("endpoint", "http://localhost:3452");
        var key = appBuilder.AddParameter("key", "secretKey", secret: true);

        appBuilder.AddConnectionString("testcs", ReferenceExpression.Create($"Endpoint={endpoint};Key={key}"));

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var connectionStringResource = Assert.Single(appModel.Resources.OfType<ConnectionStringResource>());
        var annotation = connectionStringResource.Annotations.OfType<ResourceSnapshotAnnotation>().SingleOrDefault();

        Assert.NotNull(annotation);

        var state = annotation.InitialSnapshot;

        Assert.False(state.IsHidden);
        Assert.Equal(KnownResourceTypes.ConnectionString, state.ResourceType);
        Assert.Equal(KnownResourceStates.Waiting, state.State?.Text);
    }

    [Fact]
    public void ParameterWithDescription_SetsDescriptionProperty()
    {
        // Arrange
        var appBuilder = DistributedApplication.CreateBuilder();

        // Act
        var parameter = appBuilder.AddParameter("test")
            .WithDescription("This is a test parameter");

        // Assert
        Assert.Equal("This is a test parameter", parameter.Resource.Description);
        Assert.False(parameter.Resource.EnableDescriptionMarkup);
    }

    [Fact]
    public void ParameterWithMarkdownDescription_SetsDescriptionAndMarkupProperties()
    {
        // Arrange
        var appBuilder = DistributedApplication.CreateBuilder();

        // Act
        var parameter = appBuilder.AddParameter("test")
            .WithMarkdownDescription("This is a **markdown** description");

        // Assert
        Assert.Equal("This is a **markdown** description", parameter.Resource.Description);
        Assert.True(parameter.Resource.EnableDescriptionMarkup);
    }

#pragma warning disable ASPIREINTERACTION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    [Fact]
    public void ParameterWithDescriptionAndCustomInput_AddsInputGeneratorAnnotation()
    {
        // Arrange
        var appBuilder = DistributedApplication.CreateBuilder();

        // Act
        var parameter = appBuilder.AddParameter("test")
            .WithDescription("Custom input parameter")
            .WithCustomInput(p => new InteractionInput
            {
                InputType = InputType.Number,
                Label = "Custom Label",
                Description = p.Description,
                EnableDescriptionMarkup = p.EnableDescriptionMarkup
            });

        // Assert
        Assert.Equal("Custom input parameter", parameter.Resource.Description);
        Assert.True(parameter.Resource.Annotations.OfType<InputGeneratorAnnotation>().Any());
    }

    [Fact]
    public void ParameterCreateInput_WithoutCustomGenerator_ReturnsDefaultInput()
    {
        // Arrange
        var appBuilder = DistributedApplication.CreateBuilder();
        var parameter = appBuilder.AddParameter("test")
            .WithDescription("Test description");

        // Act
        var input = parameter.Resource.CreateInput();

        // Assert
        Assert.Equal(InputType.Text, input.InputType);
        Assert.Equal("test", input.Label);
        Assert.Equal("Test description", input.Description);
        Assert.Equal("Enter value for test", input.Placeholder);
        Assert.False(input.EnableDescriptionMarkup);
    }

    [Fact]
    public void ParameterCreateInput_ForSecretParameter_ReturnsSecretTextInput()
    {
        // Arrange
        var appBuilder = DistributedApplication.CreateBuilder();
        var parameter = appBuilder.AddParameter("secret", secret: true)
            .WithDescription("Secret description");

        // Act
        var input = parameter.Resource.CreateInput();

        // Assert
        Assert.Equal(InputType.SecretText, input.InputType);
        Assert.Equal("secret", input.Label);
        Assert.Equal("Secret description", input.Description);
        Assert.Equal("Enter value for secret", input.Placeholder);
        Assert.False(input.EnableDescriptionMarkup);
    }

    [Fact]
    public void ParameterCreateInput_WithCustomGenerator_UsesGenerator()
    {
        // Arrange
        var appBuilder = DistributedApplication.CreateBuilder();
        var parameter = appBuilder.AddParameter("test")
            .WithDescription("Custom description")
            .WithCustomInput(p => new InteractionInput
            {
                InputType = InputType.Number,
                Label = "Custom Label",
                Description = "Custom: " + p.Description,
                EnableDescriptionMarkup = true,
                Placeholder = "Enter number"
            });

        // Act
        var input = parameter.Resource.CreateInput();

        // Assert
        Assert.Equal(InputType.Number, input.InputType);
        Assert.Equal("Custom Label", input.Label);
        Assert.Equal("Custom: Custom description", input.Description);
        Assert.Equal("Enter number", input.Placeholder);
        Assert.True(input.EnableDescriptionMarkup);
    }

    [Fact]
    public void ParameterCreateInput_WithMarkdownDescription_SetsMarkupFlag()
    {
        // Arrange
        var appBuilder = DistributedApplication.CreateBuilder();
        var parameter = appBuilder.AddParameter("test")
            .WithMarkdownDescription("**Bold** description");

        // Act
        var input = parameter.Resource.CreateInput();

        // Assert
        Assert.Equal("**Bold** description", input.Description);
        Assert.True(input.EnableDescriptionMarkup);
    }

    [Fact]
    public void ParameterWithCustomInput_AddsInputGeneratorAnnotation()
    {
        // Arrange
        var appBuilder = DistributedApplication.CreateBuilder();

        // Act
        var parameter = appBuilder.AddParameter("test")
            .WithCustomInput(p => new InteractionInput
            {
                InputType = InputType.Number,
                Label = "Custom Label",
                Description = "Custom description",
                EnableDescriptionMarkup = false
            });

        // Assert
        Assert.True(parameter.Resource.Annotations.OfType<InputGeneratorAnnotation>().Any());

        var input = parameter.Resource.CreateInput();
        Assert.Equal(InputType.Number, input.InputType);
        Assert.Equal("Custom Label", input.Label);
        Assert.Equal("Custom description", input.Description);
        Assert.False(input.EnableDescriptionMarkup);
    }
#pragma warning restore ASPIREINTERACTION001
}
