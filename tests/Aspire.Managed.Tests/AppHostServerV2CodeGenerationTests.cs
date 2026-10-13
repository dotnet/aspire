// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Nodes;

namespace Aspire.Managed.Tests;

public class AppHostServerV2CodeGenerationTests(ITestOutputHelper outputHelper)
{
    [Theory]
    [InlineData("provider-client.mts", true)]
    [InlineData("aspire.mts", false)]
    [InlineData("native-builder.mts", false)]
    [InlineData("../outside.mts", false)]
    public async Task PortableProviderUsesSharedProjectionWithoutReplacingTheFramework(string sdkFile, bool supported)
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var core = Path.Combine(workspace.Path, "core.json");
        var metadata = Path.Combine(workspace.Path, "metadata.json");
        var source = Path.Combine(workspace.Path, "provider.mts");
        var request = Path.Combine(workspace.Path, "request.json");
        var output = Path.Combine(workspace.Path, "generated");
        await File.WriteAllTextAsync(core, """{"Capabilities":[],"HandleTypes":[],"DtoTypes":[],"EnumTypes":[]}""");
        await File.WriteAllTextAsync(metadata, """
            {"protocolVersion":2,"capabilities":[{"id":"example.provider/greet","method":"greet",
              "parameters":[{"name":"name","type":{"typeId":"string","category":"Primitive"}}],
              "returnType":{"typeId":"string","category":"Primitive"}}]}
            """);
        await File.WriteAllTextAsync(source, "export const providerName = 'example.provider';\n");
        await File.WriteAllTextAsync(request, new JsonObject
        {
            ["coreContract"] = core,
            ["output"] = output,
            ["providers"] = new JsonArray((JsonNode)new JsonObject
            {
                ["packageName"] = "example.provider",
                ["metadata"] = metadata,
                ["sdkFiles"] = new JsonObject { [sdkFile] = source },
                ["exports"] = new JsonArray(sdkFile)
            })
        }.ToJsonString());

        if (!supported)
        {
            await Assert.ThrowsAsync<InvalidDataException>(() => AppHostServerV2CodeGeneration.GenerateAsync(request));
            return;
        }
        Assert.Equal(0, await AppHostServerV2CodeGeneration.GenerateAsync(request));
        Assert.Equal(File.ReadAllText(source), File.ReadAllText(Path.Combine(output, sdkFile)));
        await Verify(await File.ReadAllTextAsync(Path.Combine(output, "aspire.mts")), extension: "ts")
            .UseDirectory("Snapshots")
            .UseFileName("PortableProviderSdk");
    }
}
