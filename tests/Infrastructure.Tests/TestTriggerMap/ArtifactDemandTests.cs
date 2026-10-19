// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Nodes;
using Aspire.SelectTests;
using VerifyXunit;
using Xunit;

namespace Infrastructure.Tests.TestTriggerMap;

public sealed class ArtifactDemandTests
{
    private const string TestPath = "tests/Example.Tests/Test.cs";
    private static readonly string[] s_allRids = ["linux-arm64", "linux-x64", "osx-arm64", "osx-x64", "win-arm64", "win-x64"];

    [Fact]
    public async Task UnionsConsumersWithoutUpgradingPackagesToArchives()
    {
        var demand = Compute(
            tests: ["Templates.Tests", "Cli.E2E.Tests"],
            jobs: ["job:extension-e2e", "job:homebrew-installer"],
            entries:
            [
                Entry("Templates.Tests", "macos-latest", packages: true, archive: false),
                Entry("Templates.Tests", "windows-latest", packages: true, archive: false),
                Entry("Templates.Tests", "ubuntu-24.04-arm", packages: true, archive: false),
                Entry("Cli.E2E.Tests", "ubuntu-latest", packages: true, archive: true),
                Entry("Unselected.Tests", "windows-11-arm", packages: true, archive: true),
                Entry("Templates.Tests", "macos-latest", packages: true, archive: false)
            ]);

        await Verifier.Verify(demand.ToJson().ToJsonString(new JsonSerializerOptions { WriteIndented = true }), "json").UseDirectory("Snapshots");
    }

    [Fact]
    public async Task PackageFreeTestsNeedNoArtifactsEvenOnMacOS()
    {
        var demand = Compute(tests: ["Example.Tests"], entries:
        [
            Entry("Example.Tests", "macos-latest", packages: false, archive: false)
        ]);

        await Verifier.Verify(demand.ToJson().ToJsonString(new JsonSerializerOptions { WriteIndented = true }), "json").UseDirectory("Snapshots");
    }

    [Theory]
    [InlineData("ubuntu-latest", "linux-x64")]
    [InlineData("ubuntu-24.04-arm", "linux-arm64")]
    [InlineData("windows-latest", "win-x64")]
    [InlineData("windows-11-arm", "win-arm64")]
    [InlineData("macos-latest", "osx-arm64")]
    [InlineData("macos-15-intel", "osx-x64")]
    public void PackageMetadataSelectsOnlyTheConsumerRid(string runner, string rid)
    {
        var demand = Compute(tests: ["Templates.Tests"], entries:
        [
            Entry("Templates.Tests", runner, packages: true, archive: false)
        ]);

        Assert.False(demand.FullDogfood);
        Assert.True(demand.CommonFeed);
        Assert.Equal([rid], demand.RidPackages);
        Assert.Empty(demand.CliArchives);
        Assert.Empty(demand.StandaloneDashboard);
    }

    [Fact]
    public void ArchiveOnlyMetadataDoesNotInventPackageFeedDemand()
    {
        var demand = Compute(tests: ["Archive.Tests"], entries:
        [
            Entry("Archive.Tests", "ubuntu-latest", packages: false, archive: true)
        ]);

        Assert.False(demand.FullDogfood);
        Assert.False(demand.CommonFeed);
        Assert.Empty(demand.RidPackages);
        Assert.Equal(["linux-x64"], demand.CliArchives);
    }

    [Theory]
    [InlineData("job:polyglot", "linux-x64")]
    [InlineData("job:extension-e2e", "linux-x64,win-x64")]
    [InlineData("job:winget-installer", "win-arm64,win-x64")]
    [InlineData("job:homebrew-installer", "osx-arm64,osx-x64")]
    [InlineData("job:cli-starter-validation", "linux-arm64,linux-x64,osx-arm64,osx-x64,win-arm64,win-x64")]
    public void ArchiveConsumersUnionMatchingFeedsAndArchives(string job, string rids)
    {
        var demand = Compute(jobs: [job]);

        Assert.False(demand.FullDogfood);
        Assert.True(demand.CommonFeed);
        Assert.Equal(job is "job:winget-installer" or "job:homebrew-installer"
            ? Array.Empty<string>() : rids.Split(','), demand.RidPackages);
        Assert.Equal(rids.Split(','), demand.CliArchives);
        Assert.Equal(job == "job:extension-e2e", demand.Vsix);
        Assert.Equal(job switch
        {
            "job:winget-installer" => ["winget"],
            "job:homebrew-installer" => ["homebrew"],
            _ => Array.Empty<string>()
        }, demand.Installers);
    }

    [Fact]
    public void StandaloneDashboardDoesNotRequirePackageFeedsOrCliArchives()
    {
        var demand = Compute(jobs: ["job:native-dashboard-validation"]);

        Assert.False(demand.CommonFeed);
        Assert.False(demand.FullDogfood);
        Assert.Equal(s_allRids, demand.StandaloneDashboard);
        Assert.Empty(demand.RidPackages);
        Assert.Empty(demand.CliArchives);
    }

    [Fact]
    public void ExtensionUnitOnlyRequiresItsVsix()
    {
        var demand = Compute(jobs: ["job:extension-unit"]);

        Assert.True(demand.Vsix);
        Assert.False(demand.CommonFeed);
        Assert.Empty(demand.RidPackages);
        Assert.Empty(demand.CliArchives);
        Assert.Empty(demand.StandaloneDashboard);
    }

    [Fact]
    public void ActualMapDerivedInstallerDemandIsIncluded()
    {
        var map = Path.Combine(RepoRoot.Path, "eng", "github-ci", "test-trigger-map.yml");
        var selection = new TestSelector(map, ["Aspire.Acquisition.Tests"], [], new HashSet<string>())
            .Select(["tests/Aspire.Acquisition.Tests/Example.cs"], [], new SelectorOptions());
        var demand = Compute(
            jobs: selection.Jobs.ToArray(),
            tests: selection.TestProjects.ToArray(),
            entries: [Entry("Aspire.Acquisition.Tests", "ubuntu-latest", packages: false, archive: false)]);

        Assert.Equal(["homebrew", "winget"], demand.Installers);
        Assert.Equal(["osx-arm64", "osx-x64", "win-arm64", "win-x64"], demand.CliArchives);
        Assert.Empty(demand.RidPackages);
    }

    [Theory]
    [InlineData("eng/scripts/verify-cli-npm-package.ps1")]
    [InlineData("eng/scripts/verify-cli-tool-nupkg.ps1")]
    [InlineData("eng/scripts/test-native-terminalhost.ps1")]
    [InlineData("eng/scripts/tray-registration-control/control.cpp")]
    public void IgnoredProducerVerifiersStillDemandNativeOutputs(string path)
    {
        var demand = Compute(paths: [path]);

        Assert.False(demand.FullDogfood);
        Assert.True(demand.CommonFeed);
        Assert.Equal(s_allRids, demand.RidPackages);
        Assert.Equal(s_allRids, demand.CliArchives);
        Assert.Equal(s_allRids, demand.StandaloneDashboard);
    }

    [Theory]
    [InlineData("src/Aspire.Hosting/README.md")]
    [InlineData("src/Aspire.Hosting/Deleted.cs")]
    [InlineData("src/OldName/File.cs")]
    [InlineData("src/NewName/File.cs")]
    [InlineData("src/Aspire.ProjectTemplates/content/template.txt")]
    [InlineData("eng/Versions.props")]
    [InlineData("unknown/new-input")]
    [InlineData("tests/Unknown.Tests/unattributed.txt")]
    public void ShippingDeletedRenamedSharedAndUnknownInputsKeepFullDogfood(string path)
    {
        AssertFull(Compute(paths: [path], attributed: []));
    }

    [Fact]
    public void ProductionConsumerOfSharedTestInputKeepsFullDogfood()
    {
        AssertFull(Compute(paths: ["tests/Shared/Input.cs"], attributed: ["tests/Shared/Input.cs"],
            productionAffected: true));
    }

    [Fact]
    public void UnknownOrMissingSelectedMetadataFailsSafe()
    {
        AssertFull(Compute(tests: ["Missing.Tests"]));
        AssertFull(Compute(tests: ["Example.Tests"], entries:
        [
            new JsonObject { ["projectName"] = "Example.Tests" }
        ]));
        AssertFull(Compute(tests: ["Example.Tests"], entries:
        [
            Entry("Example.Tests", "custom-runner", packages: true, archive: false)
        ]));
        AssertFull(Compute(jobs: ["job:future-consumer"]));
        AssertFull(Compute(graphAvailable: false));
        AssertFull(Compute(paths: []));
    }

    [Fact]
    public async Task ForceAllKeepsEveryArtifactFamily()
    {
        var demand = Compute(selectsAll: true, paths: []);

        AssertFull(demand);
        await Verifier.Verify(demand.ToJson().ToJsonString(new JsonSerializerOptions { WriteIndented = true }), "json").UseDirectory("Snapshots");
    }

    [Theory]
    [InlineData("""{"include":{}}""")]
    [InlineData("""{"include":[{"projectName":"Example.Tests","properties":{"requiresNugets":"false","requiresCliArchive":false}}]}""")]
    public void MalformedMatrixIsAVisibleFailure(string json)
    {
        using var matrix = JsonDocument.Parse(json);

        Assert.Throws<InvalidOperationException>(() =>
            ArtifactDemand.Compute(Selection(["Example.Tests"], []), [TestPath], Affected(), true, matrix.RootElement));
    }

    private static ArtifactDemand Compute(
        string[]? tests = null,
        string[]? jobs = null,
        JsonObject[]? entries = null,
        string[]? paths = null,
        string[]? attributed = null,
        bool productionAffected = false,
        bool graphAvailable = true,
        bool selectsAll = false)
    {
        var matrix = new JsonObject { ["include"] = new JsonArray((entries ?? []).Cast<JsonNode?>().ToArray()) };
        using var document = JsonDocument.Parse(matrix.ToJsonString());
        return ArtifactDemand.Compute(Selection(tests ?? [], jobs ?? [], selectsAll),
            paths ?? [TestPath], Affected(attributed, productionAffected), graphAvailable, document.RootElement);
    }

    private static SelectionResult Selection(string[] tests, string[] jobs, bool selectsAll = false) =>
        new(selectsAll, tests.ToHashSet(StringComparer.Ordinal), jobs.ToHashSet(StringComparer.Ordinal),
            selectsAll ? "force-all" : null, new HashSet<string>(),
            new Dictionary<string, IReadOnlyList<Cause>>(), new Dictionary<string, IReadOnlyList<Cause>>());

    private static AffectedResult Affected(string[]? attributed = null, bool productionAffected = false) =>
        new(productionAffected ? ["Production"] : [],
            (attributed ?? [TestPath]).ToHashSet(StringComparer.Ordinal),
            new Dictionary<string, AffectedPath>(), new HashSet<string>());

    private static JsonObject Entry(string project, string runner, bool packages, bool archive) => new()
    {
        ["projectName"] = project,
        ["runs-on"] = runner,
        ["properties"] = new JsonObject { ["requiresNugets"] = packages, ["requiresCliArchive"] = archive }
    };

    private static void AssertFull(ArtifactDemand demand)
    {
        Assert.True(demand.FullDogfood);
        Assert.True(demand.CommonFeed);
        Assert.True(demand.Vsix);
        Assert.Equal(s_allRids, demand.RidPackages);
        Assert.Equal(s_allRids, demand.CliArchives);
        Assert.Equal(s_allRids, demand.StandaloneDashboard);
        Assert.Equal(["homebrew", "winget"], demand.Installers);
        Assert.NotEmpty(demand.Reasons);
    }
}
