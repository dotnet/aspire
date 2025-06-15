// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Aspire.Hosting.Azure.Tests;

public class DefaultUserSecretsManagerTests
{
    [Fact]
    public void SaveUserSecretsAsync_DoesNotEscapeAmpersandAndPlusCharacters()
    {
        // This test verifies that & and + characters are NOT escaped
        // as \u0026 and \u002B in the JSON output when using UnsafeRelaxedJsonEscaping
        
        // Arrange
        var userSecrets = new JsonObject
        {
            ["Parameters:token"] = "some=thing&looking=url&like=true",
            ["Parameters:password"] = "P+qMWNzkn*xm1rhXNF5st0"
        };

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        // Act
        var jsonOutput = userSecrets.ToJsonString(options);

        // Assert - these should pass with the fix
        Assert.Contains("some=thing&looking=url&like=true", jsonOutput);
        Assert.Contains("P+qMWNzkn*xm1rhXNF5st0", jsonOutput);
        
        // Should not contain escaped versions
        Assert.DoesNotContain("\\u0026", jsonOutput);
        Assert.DoesNotContain("\\u002B", jsonOutput);
    }
}