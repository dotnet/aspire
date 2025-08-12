// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Tests.Utils;
using Aspire.Hosting.Utils;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting.Tests;

public class WithEnvFileTests
{
    [Fact]
    public void EnvEntry_Constructor_SetsPropertiesCorrectly()
    {
        // Arrange & Act
        var entry = new EnvEntry("TEST_KEY", "test_value", "test comment");

        // Assert
        Assert.Equal("TEST_KEY", entry.Key);
        Assert.Equal("test_value", entry.Value);
        Assert.Equal("test comment", entry.Comment);
    }

    [Fact]
    public void EnvEntry_Constructor_WithNullValue_SetsPropertiesCorrectly()
    {
        // Arrange & Act
        var entry = new EnvEntry("TEST_KEY", null, null);

        // Assert
        Assert.Equal("TEST_KEY", entry.Key);
        Assert.Null(entry.Value);
        Assert.Null(entry.Comment);
    }

    [Fact]
    public void EnvEntry_Constructor_WithEmptyKey_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new EnvEntry("", "value", "comment"));
        Assert.Throws<ArgumentException>(() => new EnvEntry(null!, "value", "comment"));
    }

    [Fact]
    public void EnvFileParser_ParseLines_HandlesBasicKeyValuePairs()
    {
        // Arrange
        var lines = new[]
        {
            "KEY1=value1",
            "KEY2=value2"
        };

        // Act
        var entries = EnvFileParser.ParseLines(lines).ToList();

        // Assert
        Assert.Equal(2, entries.Count);
        Assert.Contains(entries, e => e.Key == "KEY1" && e.Value == "value1");
        Assert.Contains(entries, e => e.Key == "KEY2" && e.Value == "value2");
    }

    [Fact]
    public void EnvFileParser_ParseLines_HandlesQuotedValues()
    {
        // Arrange
        var lines = new[]
        {
            "KEY1=\"quoted value\"",
            "KEY2='single quoted'",
            "KEY3=\"with spaces and \\\"escaped quotes\\\"\""
        };

        // Act
        var entries = EnvFileParser.ParseLines(lines).ToList();

        // Assert
        Assert.Equal(3, entries.Count);
        Assert.Contains(entries, e => e.Key == "KEY1" && e.Value == "quoted value");
        Assert.Contains(entries, e => e.Key == "KEY2" && e.Value == "single quoted");
        Assert.Contains(entries, e => e.Key == "KEY3" && e.Value == "with spaces and \"escaped quotes\"");
    }

    [Fact]
    public void EnvFileParser_ParseLines_HandlesComments()
    {
        // Arrange
        var lines = new[]
        {
            "# This is a comment",
            "KEY1=value1",
            "# Another comment",
            "KEY2=value2"
        };

        // Act
        var entries = EnvFileParser.ParseLines(lines).ToList();

        // Assert
        Assert.Equal(2, entries.Count);
        var entry1 = entries.First(e => e.Key == "KEY1");
        var entry2 = entries.First(e => e.Key == "KEY2");
        
        Assert.Equal("This is a comment", entry1.Comment);
        Assert.Equal("Another comment", entry2.Comment);
    }

    [Fact]
    public void EnvFileParser_ParseLines_HandlesEmptyValues()
    {
        // Arrange
        var lines = new[]
        {
            "KEY1=",
            "KEY2= ",
            "KEY3=\"\""
        };

        // Act
        var entries = EnvFileParser.ParseLines(lines).ToList();

        // Assert
        Assert.Equal(3, entries.Count);
        Assert.Contains(entries, e => e.Key == "KEY1" && e.Value == "");
        Assert.Contains(entries, e => e.Key == "KEY2" && e.Value == "");
        Assert.Contains(entries, e => e.Key == "KEY3" && e.Value == "");
    }

    [Fact]
    public void EnvFileParser_ParseLines_HandlesDuplicateKeys()
    {
        // Arrange
        var lines = new[]
        {
            "KEY1=first_value",
            "KEY1=second_value"
        };

        // Act
        var entries = EnvFileParser.ParseLines(lines).ToList();

        // Assert
        Assert.Single(entries);
        Assert.Equal("KEY1", entries[0].Key);
        Assert.Equal("second_value", entries[0].Value);
    }

    [Fact]
    public void EnvFileParser_ParseLines_SkipsMalformedLines()
    {
        // Arrange
        var lines = new[]
        {
            "KEY1=value1",
            "malformed line without equals",
            "=invalid_key",
            "KEY2=value2"
        };

        // Act
        var entries = EnvFileParser.ParseLines(lines).ToList();

        // Assert
        Assert.Equal(2, entries.Count);
        Assert.Contains(entries, e => e.Key == "KEY1" && e.Value == "value1");
        Assert.Contains(entries, e => e.Key == "KEY2" && e.Value == "value2");
    }

    [Fact]
    public void EnvFileParser_ParseLines_HandlesInlineComments()
    {
        // Arrange
        var lines = new[]
        {
            "KEY1=value1 # inline comment",
            "KEY2=\"quoted value\" # another comment"
        };

        // Act
        var entries = EnvFileParser.ParseLines(lines).ToList();

        // Assert
        Assert.Equal(2, entries.Count);
        Assert.Contains(entries, e => e.Key == "KEY1" && e.Value == "value1");
        Assert.Contains(entries, e => e.Key == "KEY2" && e.Value == "quoted value");
    }

    [Fact]
    public void WithEnvFile_NonExistentFile_DoesNotThrow()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();
        var container = builder.AddContainer("test", "image");

        // Act & Assert - should not throw
        container.WithEnvFile("/non/existent/path/.env");
    }

    [Fact]
    public void WithEnvFile_CreatesParametersWithCorrectNaming()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();
        var container = builder.AddContainer("my-app", "image");

        // Create a temporary .env file
        var tempDir = Path.GetTempPath();
        var envFile = Path.Combine(tempDir, $"test-{Guid.NewGuid()}.env");
        
        try
        {
            File.WriteAllText(envFile, "API_KEY=secret123\nDATABASE_URL=postgres://...");

            // Act
            container.WithEnvFile(envFile);

            using var app = builder.Build();
            var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

            // Assert
            var parameters = appModel.Resources.OfType<ParameterResource>().ToList();
            Assert.Equal(2, parameters.Count);
            
            var apiKeyParam = parameters.SingleOrDefault(p => p.Name == "my-app-env-api_key");
            var dbUrlParam = parameters.SingleOrDefault(p => p.Name == "my-app-env-database_url");
            
            Assert.NotNull(apiKeyParam);
            Assert.NotNull(dbUrlParam);
        }
        finally
        {
            if (File.Exists(envFile))
            {
                File.Delete(envFile);
            }
        }
    }

    [Fact]
    public async Task WithEnvFile_CreatesEnvironmentVariables()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();
        var container = builder.AddContainer("test-app", "image");

        // Create a temporary .env file
        var tempDir = Path.GetTempPath();
        var envFile = Path.Combine(tempDir, $"test-{Guid.NewGuid()}.env");

        try
        {
            File.WriteAllText(envFile, "PORT=3000\nNODE_ENV=development");

            // Act
            container.WithEnvFile(envFile);

            // Get environment variables
            var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(
                container.Resource,
                DistributedApplicationOperation.Run,
                TestServiceProvider.Instance
                ).DefaultTimeout();

            // Assert
            Assert.Equal("3000", config["PORT"]);
            Assert.Equal("development", config["NODE_ENV"]);
        }
        finally
        {
            if (File.Exists(envFile))
            {
                File.Delete(envFile);
            }
        }
    }

    [Fact]
    public void WithEnvFile_ConfigureCallback_AllowsCustomization()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();
        var container = builder.AddContainer("test-app", "image");

        // Create a temporary .env file
        var tempDir = Path.GetTempPath();
        var envFile = Path.Combine(tempDir, $"test-{Guid.NewGuid()}.env");

        try
        {
            File.WriteAllText(envFile, "# Secret API key\nAPI_SECRET=secret123\n# Public API URL\nAPI_URL=https://api.example.com");

            // Act
            container.WithEnvFile((entry, paramBuilder) =>
            {
                paramBuilder.WithDescription($"Custom: {entry.Key}" + (entry.Comment != null ? $" - {entry.Comment}" : ""));
            }, envFile);

            using var app = builder.Build();
            var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

            // Assert
            var parameters = appModel.Resources.OfType<ParameterResource>().ToList();
            Assert.Equal(2, parameters.Count);
            
            var secretParam = parameters.SingleOrDefault(p => p.Name == "test-app-env-api_secret");
            var urlParam = parameters.SingleOrDefault(p => p.Name == "test-app-env-api_url");
            
            Assert.NotNull(secretParam);
            Assert.NotNull(urlParam);
            
            // SECRET should be auto-detected as secret
            Assert.True(secretParam.Secret);
            Assert.False(urlParam.Secret);
            
            Assert.Equal("Custom: API_SECRET - Secret API key", secretParam.Description);
            Assert.Equal("Custom: API_URL - Public API URL", urlParam.Description);
        }
        finally
        {
            if (File.Exists(envFile))
            {
                File.Delete(envFile);
            }
        }
    }

    [Fact]
    public void WithEnvFile_CreatesParentChildRelationship()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();
        var container = builder.AddContainer("test-app", "image");

        // Create a temporary .env file
        var tempDir = Path.GetTempPath();
        var envFile = Path.Combine(tempDir, $"test-{Guid.NewGuid()}.env");

        try
        {
            File.WriteAllText(envFile, "TEST_VAR=test_value");

            // Act
            container.WithEnvFile(envFile);

            using var app = builder.Build();
            var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

            // Assert
            var parameter = appModel.Resources.OfType<ParameterResource>().Single();
            var relationships = parameter.Annotations.OfType<ResourceRelationshipAnnotation>().ToList();
            
            Assert.Single(relationships);
            Assert.Equal(KnownRelationshipTypes.Parent, relationships[0].Type);
            Assert.Same(container.Resource, relationships[0].Resource);
        }
        finally
        {
            if (File.Exists(envFile))
            {
                File.Delete(envFile);
            }
        }
    }

    [Fact]
    public void WithEnvFile_DefaultPath_LooksInAppHostDirectory()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();
        var container = builder.AddContainer("test-app", "image");

        // Create .env file in app host directory
        var envFile = Path.Combine(builder.AppHostDirectory, ".env");
        var createdFile = false;

        try
        {
            if (!File.Exists(envFile))
            {
                File.WriteAllText(envFile, "DEFAULT_VAR=default_value");
                createdFile = true;
            }

            // Act
            container.WithEnvFile(); // No path specified

            using var app = builder.Build();
            var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

            // Assert - if .env existed, we should have parameters
            var parameters = appModel.Resources.OfType<ParameterResource>().ToList();
            if (createdFile)
            {
                Assert.Single(parameters);
                Assert.Equal("test-app-env-default_var", parameters[0].Name);
            }
        }
        finally
        {
            if (createdFile && File.Exists(envFile))
            {
                File.Delete(envFile);
            }
        }
    }

    [Fact]
    public void WithEnvFile_WithNullBuilder_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            ResourceBuilderExtensions.WithEnvFile<ContainerResource>(null!, ".env"));
    }

    [Fact]
    public void WithEnvFile_WithNullConfigure_ThrowsArgumentNullException()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();
        var container = builder.AddContainer("test", "image");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            container.WithEnvFile(configure: null!, envFilePath: ".env"));
    }

    [Fact]
    public void WithEnvFile_AutoDetectsSecrets()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();
        var container = builder.AddContainer("test-app", "image");

        // Create a temporary .env file
        var tempDir = Path.GetTempPath();
        var envFile = Path.Combine(tempDir, $"test-{Guid.NewGuid()}.env");

        try
        {
            File.WriteAllText(envFile, 
                "API_SECRET=secret123\n" +
                "DATABASE_PASSWORD=pass456\n" +
                "API_KEY=key789\n" +
                "ACCESS_TOKEN=token123\n" +
                "PUBLIC_URL=https://example.com");

            // Act
            container.WithEnvFile(envFile);

            using var app = builder.Build();
            var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

            // Assert
            var parameters = appModel.Resources.OfType<ParameterResource>().ToList();
            Assert.Equal(5, parameters.Count);
            
            var secretParam = parameters.Single(p => p.Name == "test-app-env-api_secret");
            var passwordParam = parameters.Single(p => p.Name == "test-app-env-database_password");
            var keyParam = parameters.Single(p => p.Name == "test-app-env-api_key");
            var tokenParam = parameters.Single(p => p.Name == "test-app-env-access_token");
            var urlParam = parameters.Single(p => p.Name == "test-app-env-public_url");
            
            // These should be auto-detected as secrets
            Assert.True(secretParam.Secret);
            Assert.True(passwordParam.Secret);
            Assert.True(keyParam.Secret);
            Assert.True(tokenParam.Secret);
            
            // This should not be a secret
            Assert.False(urlParam.Secret);
        }
        finally
        {
            if (File.Exists(envFile))
            {
                File.Delete(envFile);
            }
        }
    }
}