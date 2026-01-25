// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Infrastructure.Tests.Helpers;
using Xunit;

namespace Infrastructure.Tests.TestSelection;

/// <summary>
/// Tests for test selection evaluation logic.
/// These tests correspond to Test-EvaluateTestSelection.ps1.
/// </summary>
public class TestSelectionEvaluatorTests
{
    private static readonly string s_configPath = Path.Combine(
        AppContext.BaseDirectory,
        "..", "..", "..", "..", "..",
        "eng", "scripts", "test-selection-rules.json");

    private static TestSelectionEvaluator CreateEvaluator()
    {
        // Use the actual config file from the repo
        var configPath = Path.GetFullPath(s_configPath);
        return TestSelectionEvaluator.FromFile(configPath);
    }

    private static TestSelectionResult Evaluate(params string[] files)
    {
        var evaluator = CreateEvaluator();
        return evaluator.Evaluate(files);
    }

    #region Fallback Tests (F1-F6)

    [Fact]
    public void F1_EngIgnored()
    {
        // eng/ is in ignorePaths, so no tests run
        var result = Evaluate("eng/Version.Details.xml");

        Assert.False(result.RunAll);
        Assert.False(result.Categories["templates"].Enabled);
        Assert.False(result.Categories["cli_e2e"].Enabled);
        Assert.False(result.Categories["endtoend"].Enabled);
        Assert.False(result.Categories["integrations"].Enabled);
        Assert.False(result.Categories["extension"].Enabled);
    }

    [Fact]
    public void F2_DirectoryBuildProps()
    {
        // Directory.Build.props triggers all tests
        var result = Evaluate("Directory.Build.props");

        Assert.True(result.RunAll);
        Assert.True(result.Categories["templates"].Enabled);
        Assert.True(result.Categories["cli_e2e"].Enabled);
        Assert.True(result.Categories["endtoend"].Enabled);
        Assert.True(result.Categories["integrations"].Enabled);
        Assert.True(result.Categories["extension"].Enabled);
    }

    [Fact]
    public void F3_WorkflowIgnored()
    {
        // .github/workflows/ is in ignorePaths, so no tests run
        var result = Evaluate(".github/workflows/ci.yml");

        Assert.False(result.RunAll);
        Assert.False(result.Categories["templates"].Enabled);
        Assert.False(result.Categories["cli_e2e"].Enabled);
        Assert.False(result.Categories["endtoend"].Enabled);
        Assert.False(result.Categories["integrations"].Enabled);
        Assert.False(result.Categories["extension"].Enabled);
    }

    [Fact]
    public void F4_TestsSharedFallback()
    {
        // tests/Shared/** triggers all tests (in core triggerAll)
        var result = Evaluate("tests/Shared/TestHelper.cs");

        Assert.True(result.RunAll);
        Assert.True(result.Categories["templates"].Enabled);
        Assert.True(result.Categories["cli_e2e"].Enabled);
        Assert.True(result.Categories["endtoend"].Enabled);
        Assert.True(result.Categories["integrations"].Enabled);
        Assert.True(result.Categories["extension"].Enabled);
    }

    [Fact]
    public void F5_GlobalJson()
    {
        // global.json triggers all tests (in core triggerAll)
        var result = Evaluate("global.json");

        Assert.True(result.RunAll);
        Assert.True(result.Categories["templates"].Enabled);
        Assert.True(result.Categories["cli_e2e"].Enabled);
        Assert.True(result.Categories["endtoend"].Enabled);
        Assert.True(result.Categories["integrations"].Enabled);
        Assert.True(result.Categories["extension"].Enabled);
    }

    [Fact]
    public void F6_AspireSlnx()
    {
        // *.slnx triggers all tests (in core triggerAll)
        var result = Evaluate("Aspire.slnx");

        Assert.True(result.RunAll);
        Assert.True(result.Categories["templates"].Enabled);
        Assert.True(result.Categories["cli_e2e"].Enabled);
        Assert.True(result.Categories["endtoend"].Enabled);
        Assert.True(result.Categories["integrations"].Enabled);
        Assert.True(result.Categories["extension"].Enabled);
    }

    #endregion

    #region Templates Tests (T1-T2)

    [Fact]
    public void T1_TemplateSource()
    {
        var result = Evaluate("src/Aspire.ProjectTemplates/templates/aspire-starter/Program.cs");

        Assert.False(result.RunAll);
        Assert.True(result.Categories["templates"].Enabled);
        Assert.False(result.Categories["cli_e2e"].Enabled);
        Assert.False(result.Categories["endtoend"].Enabled);
        Assert.False(result.Categories["integrations"].Enabled);
        Assert.False(result.Categories["extension"].Enabled);
    }

    [Fact]
    public void T2_TemplateTest()
    {
        var result = Evaluate("tests/Aspire.Templates.Tests/TemplateTests.cs");

        Assert.False(result.RunAll);
        Assert.True(result.Categories["templates"].Enabled);
        Assert.False(result.Categories["cli_e2e"].Enabled);
        Assert.False(result.Categories["endtoend"].Enabled);
        Assert.False(result.Categories["integrations"].Enabled);
        Assert.False(result.Categories["extension"].Enabled);
    }

    #endregion

    #region CLI E2E Tests (C1-C3)

    [Fact]
    public void C1_CliSource()
    {
        var result = Evaluate("src/Aspire.Cli/Commands/NewCommand.cs");

        Assert.False(result.RunAll);
        Assert.False(result.Categories["templates"].Enabled);
        Assert.True(result.Categories["cli_e2e"].Enabled);
        Assert.False(result.Categories["endtoend"].Enabled);
        Assert.False(result.Categories["integrations"].Enabled);
        Assert.False(result.Categories["extension"].Enabled);
    }

    [Fact]
    public void C2_CliE2ETest()
    {
        var result = Evaluate("tests/Aspire.Cli.EndToEnd.Tests/NewCommandTests.cs");

        Assert.False(result.RunAll);
        Assert.False(result.Categories["templates"].Enabled);
        Assert.True(result.Categories["cli_e2e"].Enabled);
        Assert.False(result.Categories["endtoend"].Enabled);
        Assert.False(result.Categories["integrations"].Enabled);
        Assert.False(result.Categories["extension"].Enabled);
    }

    [Fact]
    public void C3_CliE2ETestNested()
    {
        var result = Evaluate("tests/Aspire.Cli.EndToEnd.Tests/Commands/SomeTest.cs");

        Assert.False(result.RunAll);
        Assert.False(result.Categories["templates"].Enabled);
        Assert.True(result.Categories["cli_e2e"].Enabled);
        Assert.False(result.Categories["endtoend"].Enabled);
        Assert.False(result.Categories["integrations"].Enabled);
        Assert.False(result.Categories["extension"].Enabled);
    }

    #endregion

    #region EndToEnd Tests (E1-E2)

    [Fact]
    public void E1_EndToEndTest()
    {
        var result = Evaluate("tests/Aspire.EndToEnd.Tests/SomeTest.cs");

        Assert.False(result.RunAll);
        Assert.False(result.Categories["templates"].Enabled);
        Assert.False(result.Categories["cli_e2e"].Enabled);
        Assert.True(result.Categories["endtoend"].Enabled);
        Assert.False(result.Categories["integrations"].Enabled);
        Assert.False(result.Categories["extension"].Enabled);
    }

    [Fact]
    public void E2_PlaygroundChange()
    {
        var result = Evaluate("playground/TestShop/TestShop.AppHost/Program.cs");

        Assert.False(result.RunAll);
        Assert.False(result.Categories["templates"].Enabled);
        Assert.False(result.Categories["cli_e2e"].Enabled);
        Assert.True(result.Categories["endtoend"].Enabled);
        Assert.False(result.Categories["integrations"].Enabled);
        Assert.False(result.Categories["extension"].Enabled);
    }

    #endregion

    #region Integrations Tests (I1-I6)

    [Fact]
    public void I1_DashboardComponent()
    {
        var result = Evaluate("src/Aspire.Dashboard/Components/Layout.razor");

        Assert.False(result.RunAll);
        Assert.False(result.Categories["templates"].Enabled);
        Assert.False(result.Categories["cli_e2e"].Enabled);
        Assert.False(result.Categories["endtoend"].Enabled);
        Assert.True(result.Categories["integrations"].Enabled);
        Assert.False(result.Categories["extension"].Enabled);
    }

    [Fact]
    public void I2_HostingSourceTriggerAll()
    {
        // src/Aspire.Hosting/** is in core triggerAll, so runs all tests
        var result = Evaluate("src/Aspire.Hosting/ApplicationModel/Resource.cs");

        Assert.True(result.RunAll);
        Assert.True(result.Categories["templates"].Enabled);
        Assert.True(result.Categories["cli_e2e"].Enabled);
        Assert.True(result.Categories["endtoend"].Enabled);
        Assert.True(result.Categories["integrations"].Enabled);
        Assert.True(result.Categories["extension"].Enabled);
    }

    [Fact]
    public void I3_DashboardTest()
    {
        var result = Evaluate("tests/Aspire.Dashboard.Tests/DashboardTests.cs");

        Assert.False(result.RunAll);
        Assert.False(result.Categories["templates"].Enabled);
        Assert.False(result.Categories["cli_e2e"].Enabled);
        Assert.False(result.Categories["endtoend"].Enabled);
        Assert.True(result.Categories["integrations"].Enabled);
        Assert.False(result.Categories["extension"].Enabled);
    }

    [Fact]
    public void I4_AzureExtension()
    {
        var result = Evaluate("src/Aspire.Hosting.Azure/AzureExtensions.cs");

        Assert.False(result.RunAll);
        Assert.False(result.Categories["templates"].Enabled);
        Assert.False(result.Categories["cli_e2e"].Enabled);
        Assert.False(result.Categories["endtoend"].Enabled);
        Assert.True(result.Categories["integrations"].Enabled);
        Assert.False(result.Categories["extension"].Enabled);
    }

    [Fact]
    public void I5_ComponentsSource()
    {
        var result = Evaluate("src/Components/SomeComponent.cs");

        Assert.False(result.RunAll);
        Assert.False(result.Categories["templates"].Enabled);
        Assert.False(result.Categories["cli_e2e"].Enabled);
        Assert.False(result.Categories["endtoend"].Enabled);
        Assert.True(result.Categories["integrations"].Enabled);
        Assert.False(result.Categories["extension"].Enabled);
    }

    [Fact]
    public void I6_SharedSource()
    {
        var result = Evaluate("src/Shared/Utils.cs");

        Assert.False(result.RunAll);
        Assert.False(result.Categories["templates"].Enabled);
        Assert.False(result.Categories["cli_e2e"].Enabled);
        Assert.False(result.Categories["endtoend"].Enabled);
        Assert.True(result.Categories["integrations"].Enabled);
        Assert.False(result.Categories["extension"].Enabled);
    }

    #endregion

    #region Extension Tests (X1-X2)

    [Fact]
    public void X1_ExtensionPackageJson()
    {
        var result = Evaluate("extension/package.json");

        Assert.False(result.RunAll);
        Assert.False(result.Categories["templates"].Enabled);
        Assert.False(result.Categories["cli_e2e"].Enabled);
        Assert.False(result.Categories["endtoend"].Enabled);
        Assert.False(result.Categories["integrations"].Enabled);
        Assert.True(result.Categories["extension"].Enabled);
    }

    [Fact]
    public void X2_ExtensionSource()
    {
        var result = Evaluate("extension/src/extension.ts");

        Assert.False(result.RunAll);
        Assert.False(result.Categories["templates"].Enabled);
        Assert.False(result.Categories["cli_e2e"].Enabled);
        Assert.False(result.Categories["endtoend"].Enabled);
        Assert.False(result.Categories["integrations"].Enabled);
        Assert.True(result.Categories["extension"].Enabled);
    }

    #endregion

    #region Multi-Category Tests (M1-M3)

    [Fact]
    public void M1_DashboardPlusExtension()
    {
        var result = Evaluate("src/Aspire.Dashboard/Foo.cs", "extension/bar.ts");

        Assert.False(result.RunAll);
        Assert.False(result.Categories["templates"].Enabled);
        Assert.False(result.Categories["cli_e2e"].Enabled);
        Assert.False(result.Categories["endtoend"].Enabled);
        Assert.True(result.Categories["integrations"].Enabled);
        Assert.True(result.Categories["extension"].Enabled);
    }

    [Fact]
    public void M2_CliPlusDashboard()
    {
        var result = Evaluate("src/Aspire.Cli/Cmd.cs", "src/Aspire.Dashboard/Foo.cs");

        Assert.False(result.RunAll);
        Assert.False(result.Categories["templates"].Enabled);
        Assert.True(result.Categories["cli_e2e"].Enabled);
        Assert.False(result.Categories["endtoend"].Enabled);
        Assert.True(result.Categories["integrations"].Enabled);
        Assert.False(result.Categories["extension"].Enabled);
    }

    [Fact]
    public void M3_TemplatesPlusPlayground()
    {
        var result = Evaluate("src/Aspire.ProjectTemplates/X.cs", "playground/Y.cs");

        Assert.False(result.RunAll);
        Assert.True(result.Categories["templates"].Enabled);
        Assert.False(result.Categories["cli_e2e"].Enabled);
        Assert.True(result.Categories["endtoend"].Enabled);
        Assert.False(result.Categories["integrations"].Enabled);
        Assert.False(result.Categories["extension"].Enabled);
    }

    #endregion

    #region Ignored Files Tests (IG1-IG3)

    [Fact]
    public void IG1_ReadmeMdIgnored()
    {
        // These are in ignorePaths, so no tests run
        var result = Evaluate("README.md");

        Assert.False(result.RunAll);
        Assert.False(result.Categories["templates"].Enabled);
        Assert.False(result.Categories["cli_e2e"].Enabled);
        Assert.False(result.Categories["endtoend"].Enabled);
        Assert.False(result.Categories["integrations"].Enabled);
        Assert.False(result.Categories["extension"].Enabled);
    }

    [Fact]
    public void IG2_DocsFolderIgnored()
    {
        var result = Evaluate("docs/getting-started.md");

        Assert.False(result.RunAll);
        Assert.False(result.Categories["templates"].Enabled);
        Assert.False(result.Categories["cli_e2e"].Enabled);
        Assert.False(result.Categories["endtoend"].Enabled);
        Assert.False(result.Categories["integrations"].Enabled);
        Assert.False(result.Categories["extension"].Enabled);
    }

    [Fact]
    public void IG3_GitignoreIgnored()
    {
        var result = Evaluate(".gitignore");

        Assert.False(result.RunAll);
        Assert.False(result.Categories["templates"].Enabled);
        Assert.False(result.Categories["cli_e2e"].Enabled);
        Assert.False(result.Categories["endtoend"].Enabled);
        Assert.False(result.Categories["integrations"].Enabled);
        Assert.False(result.Categories["extension"].Enabled);
    }

    #endregion

    #region Conservative Fallback Tests (U1-U2)

    [Fact]
    public void U1_RandomFile()
    {
        // Files not in ignorePaths and not matching any category trigger fallback
        var result = Evaluate("some-random-file.txt");

        Assert.True(result.RunAll);
        Assert.True(result.Categories["templates"].Enabled);
        Assert.True(result.Categories["cli_e2e"].Enabled);
        Assert.True(result.Categories["endtoend"].Enabled);
        Assert.True(result.Categories["integrations"].Enabled);
        Assert.True(result.Categories["extension"].Enabled);
    }

    [Fact]
    public void U2_UnknownSrcFile()
    {
        var result = Evaluate("src/Unknown/Something.cs");

        Assert.True(result.RunAll);
        Assert.True(result.Categories["templates"].Enabled);
        Assert.True(result.Categories["cli_e2e"].Enabled);
        Assert.True(result.Categories["endtoend"].Enabled);
        Assert.True(result.Categories["integrations"].Enabled);
        Assert.True(result.Categories["extension"].Enabled);
    }

    #endregion

    #region Edge Cases (EC1-EC4)

    [Fact]
    public void EC1_ReadmeInTemplatesDir()
    {
        var result = Evaluate("src/Aspire.ProjectTemplates/README.md");

        Assert.False(result.RunAll);
        Assert.True(result.Categories["templates"].Enabled);
        Assert.False(result.Categories["cli_e2e"].Enabled);
        Assert.False(result.Categories["endtoend"].Enabled);
        Assert.False(result.Categories["integrations"].Enabled);
        Assert.False(result.Categories["extension"].Enabled);
    }

    [Fact]
    public void EC2_ReadmeInCliE2EDir()
    {
        var result = Evaluate("tests/Aspire.Cli.EndToEnd.Tests/README.md");

        Assert.False(result.RunAll);
        Assert.False(result.Categories["templates"].Enabled);
        Assert.True(result.Categories["cli_e2e"].Enabled);
        Assert.False(result.Categories["endtoend"].Enabled);
        Assert.False(result.Categories["integrations"].Enabled);
        Assert.False(result.Categories["extension"].Enabled);
    }

    [Fact]
    public void EC3_NewAspireProjectNotExcluded()
    {
        var result = Evaluate("src/Aspire.Cli.SomeNew/Foo.cs");

        Assert.False(result.RunAll);
        Assert.False(result.Categories["templates"].Enabled);
        Assert.False(result.Categories["cli_e2e"].Enabled);
        Assert.False(result.Categories["endtoend"].Enabled);
        Assert.True(result.Categories["integrations"].Enabled);
        Assert.False(result.Categories["extension"].Enabled);
    }

    [Fact]
    public void EC4_NoChanges()
    {
        var result = Evaluate();

        Assert.False(result.RunAll);
        Assert.Equal("no_changes", result.TriggerReason);
        Assert.False(result.Categories["templates"].Enabled);
        Assert.False(result.Categories["cli_e2e"].Enabled);
        Assert.False(result.Categories["endtoend"].Enabled);
        Assert.False(result.Categories["integrations"].Enabled);
        Assert.False(result.Categories["extension"].Enabled);
    }

    #endregion

    #region Exclude Pattern Tests (EX1-EX3)

    [Fact]
    public void EX1_TemplatesExcludedFromIntegrations()
    {
        var result = Evaluate("src/Aspire.ProjectTemplates/Foo.cs");

        Assert.False(result.RunAll);
        Assert.True(result.Categories["templates"].Enabled);
        Assert.False(result.Categories["cli_e2e"].Enabled);
        Assert.False(result.Categories["endtoend"].Enabled);
        Assert.False(result.Categories["integrations"].Enabled);
        Assert.False(result.Categories["extension"].Enabled);
    }

    [Fact]
    public void EX2_CliExcludedFromIntegrations()
    {
        var result = Evaluate("src/Aspire.Cli/Bar.cs");

        Assert.False(result.RunAll);
        Assert.False(result.Categories["templates"].Enabled);
        Assert.True(result.Categories["cli_e2e"].Enabled);
        Assert.False(result.Categories["endtoend"].Enabled);
        Assert.False(result.Categories["integrations"].Enabled);
        Assert.False(result.Categories["extension"].Enabled);
    }

    [Fact]
    public void EX3_TemplateTestsExcludedFromIntegrations()
    {
        var result = Evaluate("tests/Aspire.Templates.Tests/X.cs");

        Assert.False(result.RunAll);
        Assert.True(result.Categories["templates"].Enabled);
        Assert.False(result.Categories["cli_e2e"].Enabled);
        Assert.False(result.Categories["endtoend"].Enabled);
        Assert.False(result.Categories["integrations"].Enabled);
        Assert.False(result.Categories["extension"].Enabled);
    }

    #endregion

    #region Project Mapping Tests (PM1-PM7)

    [Fact]
    public void PM1_ComponentsMapping()
    {
        var result = Evaluate("src/Components/Aspire.Microsoft.Data.SqlClient/SqlClientExtensions.cs");

        Assert.False(result.RunAll);
        Assert.True(result.Categories["integrations"].Enabled);
        Assert.Contains("tests/Aspire.Microsoft.Data.SqlClient.Tests/", result.Projects);
    }

    [Fact]
    public void PM2_AspireHostingXMapping()
    {
        var result = Evaluate("src/Aspire.Hosting.Redis/RedisBuilderExtensions.cs");

        Assert.False(result.RunAll);
        Assert.True(result.Categories["integrations"].Enabled);
        Assert.Contains("tests/Aspire.Hosting.Redis.Tests/", result.Projects);
    }

    [Fact]
    public void PM3_AspireHostingTestingExcludedFromMapping()
    {
        var result = Evaluate("src/Aspire.Hosting.Testing/DistributedApplicationTestingBuilder.cs");

        Assert.False(result.RunAll);
        Assert.True(result.Categories["integrations"].Enabled);
        Assert.DoesNotContain("tests/Aspire.Hosting.Testing.Tests/", result.Projects);
    }

    [Fact]
    public void PM4_TestProjectSelfMapping()
    {
        var result = Evaluate("tests/Aspire.Dashboard.Tests/DashboardTests.cs");

        Assert.False(result.RunAll);
        Assert.True(result.Categories["integrations"].Enabled);
        Assert.Contains("tests/Aspire.Dashboard.Tests/", result.Projects);
    }

    [Fact]
    public void PM5_MultipleFilesMultipleMappings()
    {
        var result = Evaluate(
            "src/Components/Aspire.Npgsql/NpgsqlExtensions.cs",
            "src/Aspire.Hosting.PostgreSQL/PostgreSQLExtensions.cs");

        Assert.False(result.RunAll);
        Assert.True(result.Categories["integrations"].Enabled);
        Assert.Contains("tests/Aspire.Npgsql.Tests/", result.Projects);
        Assert.Contains("tests/Aspire.Hosting.PostgreSQL.Tests/", result.Projects);
    }

    [Fact]
    public void PM6_AspireHostingAzureMapping()
    {
        var result = Evaluate("src/Aspire.Hosting.Azure/AzureExtensions.cs");

        Assert.False(result.RunAll);
        Assert.True(result.Categories["integrations"].Enabled);
        Assert.Contains("tests/Aspire.Hosting.Azure.Tests/", result.Projects);
    }

    [Fact]
    public void PM7_NestedPathInAspireHostingX()
    {
        var result = Evaluate("src/Aspire.Hosting.Milvus/MilvusBuilderExtensions.cs");

        Assert.False(result.RunAll);
        Assert.True(result.Categories["integrations"].Enabled);
        Assert.Contains("tests/Aspire.Hosting.Milvus.Tests/", result.Projects);
    }

    #endregion

    #region Projects Array Format Tests (PAF1-PAF8)

    [Fact]
    public void PAF1_SingleComponentsProject()
    {
        var result = Evaluate("src/Components/Aspire.Milvus.Client/MilvusExtensions.cs");

        Assert.Single(result.Projects);
        Assert.Contains("tests/Aspire.Milvus.Client.Tests/", result.Projects);
    }

    [Fact]
    public void PAF2_MultipleComponentsProjects()
    {
        var result = Evaluate(
            "src/Components/Aspire.Npgsql/NpgsqlExtensions.cs",
            "src/Components/Aspire.StackExchange.Redis/RedisExtensions.cs");

        Assert.Equal(2, result.Projects.Count);
        Assert.Contains("tests/Aspire.Npgsql.Tests/", result.Projects);
        Assert.Contains("tests/Aspire.StackExchange.Redis.Tests/", result.Projects);
    }

    [Fact]
    public void PAF3_RunAllTrueHasEmptyProjects()
    {
        var result = Evaluate("src/Aspire.Hosting/Something.cs");

        Assert.True(result.RunAll);
        Assert.Empty(result.Projects);
    }

    [Fact]
    public void PAF4_NoChangesHasEmptyProjects()
    {
        var result = Evaluate();

        Assert.False(result.RunAll);
        Assert.Empty(result.Projects);
    }

    [Fact]
    public void PAF5_IgnoredFilesHasEmptyProjects()
    {
        var result = Evaluate("docs/readme.md");

        Assert.False(result.RunAll);
        Assert.Empty(result.Projects);
    }

    [Fact]
    public void PAF6_HostingXProjectMapping()
    {
        var result = Evaluate("src/Aspire.Hosting.Redis/RedisBuilderExtensions.cs");

        Assert.Single(result.Projects);
        Assert.Contains("tests/Aspire.Hosting.Redis.Tests/", result.Projects);
    }

    [Fact]
    public void PAF7_TestFileSelfMapping()
    {
        var result = Evaluate("tests/Aspire.Milvus.Client.Tests/MilvusTests.cs");

        Assert.Single(result.Projects);
        Assert.Contains("tests/Aspire.Milvus.Client.Tests/", result.Projects);
    }

    [Fact]
    public void PAF8_MixedSourceAndTestChanges()
    {
        var result = Evaluate(
            "src/Components/Aspire.Npgsql/Npgsql.cs",
            "tests/Aspire.Hosting.Redis.Tests/RedisTests.cs");

        Assert.Equal(2, result.Projects.Count);
        Assert.Contains("tests/Aspire.Npgsql.Tests/", result.Projects);
        Assert.Contains("tests/Aspire.Hosting.Redis.Tests/", result.Projects);
    }

    #endregion
}
