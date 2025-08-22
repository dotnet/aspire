// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml.Linq;
using Aspire.Cli.Packaging;
using Aspire.Cli.Tests.Utils;

namespace Aspire.Cli.Tests.Packaging;

public class NuGetConfigMergerTests
{
    private readonly ITestOutputHelper _outputHelper;

    public NuGetConfigMergerTests(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;
    }

    private static FileInfo WriteConfig(DirectoryInfo dir, string fileName, string content)
    {
        var path = Path.Combine(dir.FullName, fileName);
        File.WriteAllText(path, content);
        return new FileInfo(path);
    }

    [Fact]
    public async Task CreateOrUpdateAsync_CopiesTemporaryConfig_WhenNoExistingConfig()
    {
        using var workspace = TemporaryWorkspace.Create(_outputHelper);
        var root = workspace.WorkspaceRoot;

        var mappings = new[]
        {
            new PackageMapping("Aspire.*", "https://feed1.example"),
            new PackageMapping(PackageMapping.AllPackages, "https://feed2.example")
        };

        using var tempConfig = await TemporaryNuGetConfig.CreateAsync(mappings);

        await NuGetConfigMerger.CreateOrUpdateAsync(root, tempConfig, mappings);

        var targetConfigPath = Path.Combine(root.FullName, "NuGet.config");
        Assert.True(File.Exists(targetConfigPath));

        var expected = await File.ReadAllTextAsync(tempConfig.ConfigFile.FullName);
        var actual = await File.ReadAllTextAsync(targetConfigPath);
        Assert.Equal(NormalizeLineEndings(expected), NormalizeLineEndings(actual));
    }

    [Fact]
    public async Task CreateOrUpdateAsync_GeneratesConfigFromMappings_WhenNoTempConfig()
    {
        using var workspace = TemporaryWorkspace.Create(_outputHelper);
        var root = workspace.WorkspaceRoot;

        var mappings = new[]
        {
            new PackageMapping("Aspire.*", "https://feed1.example"),
            new PackageMapping(PackageMapping.AllPackages, "https://feed2.example")
        };

        await NuGetConfigMerger.CreateOrUpdateAsync(root, temporaryConfig: null, mappings);

        var targetConfigPath = Path.Combine(root.FullName, "NuGet.config");
        Assert.True(File.Exists(targetConfigPath));

        var xml = XDocument.Load(targetConfigPath);
        var packageSources = xml.Root!.Element("packageSources")!;
        Assert.Contains(packageSources.Elements("add"), e => (string?)e.Attribute("value") == "https://feed1.example");
        Assert.Contains(packageSources.Elements("add"), e => (string?)e.Attribute("value") == "https://feed2.example");

        var psm = xml.Root!.Element("packageSourceMapping");
        Assert.NotNull(psm);
        Assert.Equal(2, psm!.Elements("packageSource").Count());
    }

    [Fact]
    public async Task CreateOrUpdateAsync_AddsMissingSources_WhenUpdatingExistingConfig()
    {
        using var workspace = TemporaryWorkspace.Create(_outputHelper);
        var root = workspace.WorkspaceRoot;

        // Existing config with one source only
    WriteConfig(root, "NuGet.config", """<?xml version="1.0"?><configuration><packageSources><add key="https://feed1.example" value="https://feed1.example" /></packageSources><packageSourceMapping><packageSource key="https://feed1.example"><package pattern="Aspire.*" /></packageSource></packageSourceMapping></configuration>""");

        var mappings = new[]
        {
            new PackageMapping("Aspire.*", "https://feed1.example"),
            new PackageMapping("Microsoft.*", "https://feed2.example") // feed2 missing
        };

        await NuGetConfigMerger.CreateOrUpdateAsync(root, temporaryConfig: null, mappings);

        var xml = XDocument.Load(Path.Combine(root.FullName, "NuGet.config"));
        var packageSources = xml.Root!.Element("packageSources")!;
        Assert.Contains(packageSources.Elements("add"), e => (string?)e.Attribute("value") == "https://feed2.example");

        // Ensure existing mapping retained
        var psm = xml.Root!.Element("packageSourceMapping")!;
        Assert.NotNull(psm.Elements("packageSource").First().Elements("package").FirstOrDefault(p => (string?)p.Attribute("pattern") == "Aspire.*"));
    }

    [Fact]
    public async Task CreateOrUpdateAsync_RemapsPatternsAndRemovesEmptySources()
    {
        using var workspace = TemporaryWorkspace.Create(_outputHelper);
        var root = workspace.WorkspaceRoot;

        // Existing config: pattern Lib.* mapped to old source only
    WriteConfig(root, "NuGet.config", """<?xml version="1.0"?><configuration><packageSources><add key="https://old.example" value="https://old.example" /></packageSources><packageSourceMapping><packageSource key="https://old.example"><package pattern="Lib.*" /></packageSource></packageSourceMapping></configuration>""");

        var mappings = new[]
        {
            new PackageMapping("Lib.*", "https://new.example")
        };

        await NuGetConfigMerger.CreateOrUpdateAsync(root, temporaryConfig: null, mappings);

        var xml = XDocument.Load(Path.Combine(root.FullName, "NuGet.config"));
        var packageSources = xml.Root!.Element("packageSources")!;
        // Old source should be removed because it's no longer used
        Assert.DoesNotContain(packageSources.Elements("add"), e => (string?)e.Attribute("value") == "https://old.example");
        // New source should be present
        Assert.Contains(packageSources.Elements("add"), e => (string?)e.Attribute("value") == "https://new.example");

        var psm = xml.Root!.Element("packageSourceMapping")!;
        Assert.Single(psm.Elements("packageSource"));
        Assert.Equal("https://new.example", (string?)psm.Element("packageSource")!.Attribute("key"));
        Assert.Equal("Lib.*", (string?)psm.Element("packageSource")!.Element("package")!.Attribute("pattern"));
    }

    [Fact]
    public async Task CreateOrUpdateAsync_CreatesPackageSourceMapping_WhenAbsent()
    {
        using var workspace = TemporaryWorkspace.Create(_outputHelper);
        var root = workspace.WorkspaceRoot;

        // Existing config without packageSourceMapping
    WriteConfig(root, "nuget.config", """<?xml version="1.0"?><configuration><packageSources><add key="https://feed1.example" value="https://feed1.example" /><add key="https://feed2.example" value="https://feed2.example" /></packageSources></configuration>""");

        var mappings = new[]
        {
            new PackageMapping("Aspire.*", "https://feed1.example"),
            new PackageMapping("Microsoft.*", "https://feed2.example")
        };

        await NuGetConfigMerger.CreateOrUpdateAsync(root, temporaryConfig: null, mappings);

        var xml = XDocument.Load(Path.Combine(root.FullName, "nuget.config"));
        var psm = xml.Root!.Element("packageSourceMapping");
        Assert.NotNull(psm);
        Assert.Equal(2, psm!.Elements("packageSource").Count());
    }

    [Fact]
    public void HasMissingSources_ReturnsTrue_WhenConfigAbsent()
    {
        using var workspace = TemporaryWorkspace.Create(_outputHelper);
        var root = workspace.WorkspaceRoot;
        var mappings = new[] { new PackageMapping("Aspire.*", "https://feed.example") };
        Assert.True(NuGetConfigMerger.HasMissingSources(root, mappings));
    }

    [Fact]
    public void HasMissingSources_ReturnsTrue_WhenPatternMappedToWrongSource()
    {
        using var workspace = TemporaryWorkspace.Create(_outputHelper);
        var root = workspace.WorkspaceRoot;

    WriteConfig(root, "NuGet.config", """<?xml version="1.0"?><configuration><packageSources><add key="https://feed1.example" value="https://feed1.example" /><add key="https://feed2.example" value="https://feed2.example" /></packageSources><packageSourceMapping><packageSource key="https://feed1.example"><package pattern="Aspire.*" /></packageSource></packageSourceMapping></configuration>""");

        var mappings = new[]
        {
            new PackageMapping("Aspire.*", "https://feed2.example") // should be feed2, but config has feed1
        };

        Assert.True(NuGetConfigMerger.HasMissingSources(root, mappings));
    }

    [Fact]
    public void HasMissingSources_ReturnsFalse_WhenAllSourcesAndMappingsPresent()
    {
        using var workspace = TemporaryWorkspace.Create(_outputHelper);
        var root = workspace.WorkspaceRoot;

    WriteConfig(root, "NuGet.config", """<?xml version="1.0"?><configuration><packageSources><add key="https://feed1.example" value="https://feed1.example" /><add key="https://feed2.example" value="https://feed2.example" /></packageSources><packageSourceMapping><packageSource key="https://feed1.example"><package pattern="Aspire.*" /></packageSource><packageSource key="https://feed2.example"><package pattern="Microsoft.*" /></packageSource></packageSourceMapping></configuration>""");

        var mappings = new[]
        {
            new PackageMapping("Aspire.*", "https://feed1.example"),
            new PackageMapping("Microsoft.*", "https://feed2.example")
        };

        Assert.False(NuGetConfigMerger.HasMissingSources(root, mappings));
    }

    private static string NormalizeLineEndings(string text) => text.Replace("\r\n", "\n");
}
