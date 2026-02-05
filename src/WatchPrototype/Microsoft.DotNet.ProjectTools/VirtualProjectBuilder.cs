// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Security;
using System.Xml;
using Microsoft.Build.Construction;
using Microsoft.Build.Definition;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.CodeAnalysis.Text;
using Microsoft.DotNet.FileBasedPrograms;
using Microsoft.DotNet.Utilities;

namespace Microsoft.DotNet.ProjectTools;

internal sealed class VirtualProjectBuilder
{
    private readonly IEnumerable<(string name, string value)> _defaultProperties;

    public string EntryPointFileFullPath { get; }

    public SourceFile EntryPointSourceFile
    {
        get
        {
            if (field == default)
            {
                field = SourceFile.Load(EntryPointFileFullPath);
            }

            return field;
        }
    }

    public string ArtifactsPath
        => field ??= GetArtifactsPath(EntryPointFileFullPath);

    public string[]? RequestedTargets { get; }

    public VirtualProjectBuilder(
        string entryPointFileFullPath,
        string targetFramework,
        string[]? requestedTargets = null,
        string? artifactsPath = null)
    {
        Debug.Assert(Path.IsPathFullyQualified(entryPointFileFullPath));

        EntryPointFileFullPath = entryPointFileFullPath;
        RequestedTargets = requestedTargets;
        ArtifactsPath = artifactsPath;
        _defaultProperties = GetDefaultProperties(targetFramework);
    }

    /// <remarks>
    /// Kept in sync with the default <c>dotnet new console</c> project file (enforced by <c>DotnetProjectConvertTests.SameAsTemplate</c>).
    /// </remarks>
    public static IEnumerable<(string name, string value)> GetDefaultProperties(string targetFramework) =>
    [
        ("OutputType", "Exe"),
        ("TargetFramework", targetFramework),
        ("ImplicitUsings", "enable"),
        ("Nullable", "enable"),
        ("PublishAot", "true"),
        ("PackAsTool", "true"),
    ];

    public static string GetArtifactsPath(string entryPointFileFullPath)
    {
        // Include entry point file name so the directory name is not completely opaque.
        string fileName = Path.GetFileNameWithoutExtension(entryPointFileFullPath);
        string hash = Sha256Hasher.HashWithNormalizedCasing(entryPointFileFullPath);
        string directoryName = $"{fileName}-{hash}";

        return GetTempSubpath(directoryName);
    }

    public static string GetVirtualProjectPath(string entryPointFilePath)
        => Path.ChangeExtension(entryPointFilePath, ".csproj");

    /// <summary>
    /// Obtains a temporary subdirectory for file-based app artifacts, e.g., <c>/tmp/dotnet/runfile/</c>.
    /// </summary>
    public static string GetTempSubdirectory()
    {
        // We want a location where permissions are expected to be restricted to the current user.
        string directory = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? Path.GetTempPath()
            : Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        if (string.IsNullOrEmpty(directory))
        {
            throw new InvalidOperationException(FileBasedProgramsResources.EmptyTempPath);
        }

        return Path.Join(directory, "dotnet", "runfile");
    }

    /// <summary>
    /// Obtains a specific temporary path in a subdirectory for file-based app artifacts, e.g., <c>/tmp/dotnet/runfile/{name}</c>.
    /// </summary>
    public static string GetTempSubpath(string name)
    {
        return Path.Join(GetTempSubdirectory(), name);
    }

    public static bool IsValidEntryPointPath(string entryPointFilePath)
    {
        if (!File.Exists(entryPointFilePath))
        {
            return false;
        }

        if (entryPointFilePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Check if the first two characters are #!
        try
        {
            using var stream = File.OpenRead(entryPointFilePath);
            int first = stream.ReadByte();
            int second = stream.ReadByte();
            return first == '#' && second == '!';
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// If there are any <c>#:project</c> <paramref name="directives"/>,
    /// evaluates their values as MSBuild expressions (i.e. substitutes <c>$()</c> and <c>@()</c> with property and item values, etc.) and
    /// resolves the evaluated values to full project file paths (e.g. if the evaluted value is a directory finds a project in that directory).
    /// </summary>
    internal static ImmutableArray<CSharpDirective> EvaluateDirectives(
        ProjectInstance? project,
        ImmutableArray<CSharpDirective> directives,
        SourceFile sourceFile,
        ErrorReporter errorReporter)
    {
        if (directives.OfType<CSharpDirective.Project>().Any())
        {
            return directives
                .Select(d => d is CSharpDirective.Project p
                    ? (project is null
                        ? p
                        : p.WithName(project.ExpandString(p.Name), CSharpDirective.Project.NameKind.Expanded))
                       .EnsureProjectFilePath(sourceFile, errorReporter)
                    : d)
                .ToImmutableArray();
        }

        return directives;
    }

    public void CreateProjectInstance(
        ProjectCollection projectCollection,
        ErrorReporter errorReporter,
        out ProjectInstance project,
        out ImmutableArray<CSharpDirective> evaluatedDirectives,
        ImmutableArray<CSharpDirective> directives = default,
        Action<IDictionary<string, string>>? addGlobalProperties = null,
        bool validateAllDirectives = false)
    {
        if (directives.IsDefault)
        {
            directives = FileLevelDirectiveHelpers.FindDirectives(EntryPointSourceFile, validateAllDirectives, errorReporter);
        }

        project = CreateProjectInstance(projectCollection, directives, addGlobalProperties);

        evaluatedDirectives = EvaluateDirectives(project, directives, EntryPointSourceFile, errorReporter);
        if (evaluatedDirectives != directives)
        {
            project = CreateProjectInstance(projectCollection, evaluatedDirectives, addGlobalProperties);
        }
    }

    private ProjectInstance CreateProjectInstance(
        ProjectCollection projectCollection,
        ImmutableArray<CSharpDirective> directives,
        Action<IDictionary<string, string>>? addGlobalProperties = null)
    {
        var projectRoot = CreateProjectRootElement(projectCollection);

        var globalProperties = projectCollection.GlobalProperties;
        if (addGlobalProperties is not null)
        {
            globalProperties = new Dictionary<string, string>(projectCollection.GlobalProperties, StringComparer.OrdinalIgnoreCase);
            addGlobalProperties(globalProperties);
        }

        return ProjectInstance.FromProjectRootElement(projectRoot, new ProjectOptions
        {
            ProjectCollection = projectCollection,
            GlobalProperties = globalProperties,
        });

        ProjectRootElement CreateProjectRootElement(ProjectCollection projectCollection)
        {
            var projectFileFullPath = GetVirtualProjectPath(EntryPointFileFullPath);
            var projectFileWriter = new StringWriter();

            WriteProjectFile(
                projectFileWriter,
                directives,
                _defaultProperties,
                isVirtualProject: true,
                targetFilePath: EntryPointFileFullPath,
                artifactsPath: ArtifactsPath,
                includeRuntimeConfigInformation: RequestedTargets?.ContainsAny("Publish", "Pack") != true);

            var projectFileText = projectFileWriter.ToString();

            using var reader = new StringReader(projectFileText);
            using var xmlReader = XmlReader.Create(reader);
            var projectRoot = ProjectRootElement.Create(xmlReader, projectCollection);
            projectRoot.FullPath = projectFileFullPath;
            return projectRoot;
        }
    }

    public static void WriteProjectFile(
        TextWriter writer,
        ImmutableArray<CSharpDirective> directives,
        IEnumerable<(string name, string value)> defaultProperties,
        bool isVirtualProject,
        string? targetFilePath = null,
        string? artifactsPath = null,
        bool includeRuntimeConfigInformation = true,
        string? userSecretsId = null)
    {
        Debug.Assert(userSecretsId == null || !isVirtualProject);

        int processedDirectives = 0;

        var sdkDirectives = directives.OfType<CSharpDirective.Sdk>();
        var propertyDirectives = directives.OfType<CSharpDirective.Property>();
        var packageDirectives = directives.OfType<CSharpDirective.Package>();
        var projectDirectives = directives.OfType<CSharpDirective.Project>();

        const string defaultSdkName = "Microsoft.NET.Sdk";
        string firstSdkName;
        string? firstSdkVersion;

        if (sdkDirectives.FirstOrDefault() is { } firstSdk)
        {
            firstSdkName = firstSdk.Name;
            firstSdkVersion = firstSdk.Version;
            processedDirectives++;
        }
        else
        {
            firstSdkName = defaultSdkName;
            firstSdkVersion = null;
        }

        if (isVirtualProject)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(artifactsPath));

            // Note that ArtifactsPath needs to be specified before Sdk.props
            // (usually it's recommended to specify it in Directory.Build.props
            // but importing Sdk.props manually afterwards also works).
            writer.WriteLine($"""
                <Project>

                  <PropertyGroup>
                    <IncludeProjectNameInArtifactsPaths>false</IncludeProjectNameInArtifactsPaths>
                    <ArtifactsPath>{EscapeValue(artifactsPath)}</ArtifactsPath>
                    <PublishDir>artifacts/$(MSBuildProjectName)</PublishDir>
                    <PackageOutputPath>artifacts/$(MSBuildProjectName)</PackageOutputPath>
                    <FileBasedProgram>true</FileBasedProgram>
                    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
                    <DisableDefaultItemsInProjectFolder>true</DisableDefaultItemsInProjectFolder>
                """);

            // Only set these to false when using the default SDK with no additional SDKs
            // to avoid including .resx and other files that are typically not expected in simple file-based apps.
            // When other SDKs are used (e.g., Microsoft.NET.Sdk.Web), keep the default behavior.
            bool usingOnlyDefaultSdk = firstSdkName == defaultSdkName && sdkDirectives.Count() <= 1;
            if (usingOnlyDefaultSdk)
            {
                writer.WriteLine($"""
                        <EnableDefaultEmbeddedResourceItems>false</EnableDefaultEmbeddedResourceItems>
                        <EnableDefaultNoneItems>false</EnableDefaultNoneItems>
                    """);
            }

            // Write default properties before importing SDKs so they can be overridden by SDKs
            // (and implicit build files which are imported by the default .NET SDK).
            foreach (var (name, value) in defaultProperties)
            {
                writer.WriteLine($"""
                        <{name}>{EscapeValue(value)}</{name}>
                    """);
            }

            writer.WriteLine($"""
                  </PropertyGroup>

                  <ItemGroup>
                    <Clean Include="{EscapeValue(artifactsPath)}/*" />
                  </ItemGroup>

                """);

            if (firstSdkVersion is null)
            {
                writer.WriteLine($"""
                      <Import Project="Sdk.props" Sdk="{EscapeValue(firstSdkName)}" />
                    """);
            }
            else
            {
                writer.WriteLine($"""
                      <Import Project="Sdk.props" Sdk="{EscapeValue(firstSdkName)}" Version="{EscapeValue(firstSdkVersion)}" />
                    """);
            }
        }
        else
        {
            string slashDelimited = firstSdkVersion is null
                ? firstSdkName
                : $"{firstSdkName}/{firstSdkVersion}";
            writer.WriteLine($"""
                <Project Sdk="{EscapeValue(slashDelimited)}">

                """);
        }

        foreach (var sdk in sdkDirectives.Skip(1))
        {
            if (isVirtualProject)
            {
                WriteImport(writer, "Sdk.props", sdk);
            }
            else if (sdk.Version is null)
            {
                writer.WriteLine($"""
                      <Sdk Name="{EscapeValue(sdk.Name)}" />
                    """);
            }
            else
            {
                writer.WriteLine($"""
                      <Sdk Name="{EscapeValue(sdk.Name)}" Version="{EscapeValue(sdk.Version)}" />
                    """);
            }

            processedDirectives++;
        }

        if (isVirtualProject || processedDirectives > 1)
        {
            writer.WriteLine();
        }

        // Write default and custom properties.
        {
            writer.WriteLine("""
                  <PropertyGroup>
                """);

            // First write the default properties except those specified by the user.
            if (!isVirtualProject)
            {
                var customPropertyNames = propertyDirectives
                    .Select(static d => d.Name)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                foreach (var (name, value) in defaultProperties)
                {
                    if (!customPropertyNames.Contains(name))
                    {
                        writer.WriteLine($"""
                                <{name}>{EscapeValue(value)}</{name}>
                            """);
                    }
                }

                if (userSecretsId != null && !customPropertyNames.Contains("UserSecretsId"))
                {
                    writer.WriteLine($"""
                            <UserSecretsId>{EscapeValue(userSecretsId)}</UserSecretsId>
                        """);
                }
            }

            // Write custom properties.
            foreach (var property in propertyDirectives)
            {
                writer.WriteLine($"""
                        <{property.Name}>{EscapeValue(property.Value)}</{property.Name}>
                    """);

                processedDirectives++;
            }

            // Write virtual-only properties which cannot be overridden.
            if (isVirtualProject)
            {
                writer.WriteLine("""
                        <RestoreUseStaticGraphEvaluation>false</RestoreUseStaticGraphEvaluation>
                        <Features>$(Features);FileBasedProgram</Features>
                    """);
            }

            writer.WriteLine("""
                  </PropertyGroup>

                """);
        }

        if (packageDirectives.Any())
        {
            writer.WriteLine("""
                  <ItemGroup>
                """);

            foreach (var package in packageDirectives)
            {
                if (package.Version is null)
                {
                    writer.WriteLine($"""
                            <PackageReference Include="{EscapeValue(package.Name)}" />
                        """);
                }
                else
                {
                    writer.WriteLine($"""
                            <PackageReference Include="{EscapeValue(package.Name)}" Version="{EscapeValue(package.Version)}" />
                        """);
                }

                processedDirectives++;
            }

            writer.WriteLine("""
                  </ItemGroup>

                """);
        }

        if (projectDirectives.Any())
        {
            writer.WriteLine("""
                  <ItemGroup>
                """);

            foreach (var projectReference in projectDirectives)
            {
                writer.WriteLine($"""
                        <ProjectReference Include="{EscapeValue(projectReference.Name)}" />
                    """);

                processedDirectives++;
            }

            writer.WriteLine("""
                  </ItemGroup>

                """);
        }

        Debug.Assert(processedDirectives + directives.OfType<CSharpDirective.Shebang>().Count() == directives.Length);

        if (isVirtualProject)
        {
            Debug.Assert(targetFilePath is not null);

            // Only add explicit Compile item when EnableDefaultCompileItems is not true.
            // When EnableDefaultCompileItems=true, the file is included via default MSBuild globbing.
            // See https://github.com/dotnet/sdk/issues/51785
            writer.WriteLine($"""
                  <ItemGroup>
                    <Compile Condition="'$(EnableDefaultCompileItems)' != 'true'" Include="{EscapeValue(targetFilePath)}" />
                  </ItemGroup>

                """);

            if (includeRuntimeConfigInformation)
            {
                var targetDirectory = Path.GetDirectoryName(targetFilePath) ?? "";
                writer.WriteLine($"""
                      <ItemGroup>
                        <RuntimeHostConfigurationOption Include="EntryPointFilePath" Value="{EscapeValue(targetFilePath)}" />
                        <RuntimeHostConfigurationOption Include="EntryPointFileDirectoryPath" Value="{EscapeValue(targetDirectory)}" />
                      </ItemGroup>

                    """);
            }

            foreach (var sdk in sdkDirectives)
            {
                WriteImport(writer, "Sdk.targets", sdk);
            }

            if (!sdkDirectives.Any())
            {
                Debug.Assert(firstSdkName == defaultSdkName && firstSdkVersion == null);
                writer.WriteLine($"""
                      <Import Project="Sdk.targets" Sdk="{defaultSdkName}" />
                    """);
            }

            writer.WriteLine();
        }

        writer.WriteLine("""
            </Project>
            """);

        static string EscapeValue(string value) => SecurityElement.Escape(value);

        static void WriteImport(TextWriter writer, string project, CSharpDirective.Sdk sdk)
        {
            if (sdk.Version is null)
            {
                writer.WriteLine($"""
                      <Import Project="{EscapeValue(project)}" Sdk="{EscapeValue(sdk.Name)}" />
                    """);
            }
            else
            {
                writer.WriteLine($"""
                      <Import Project="{EscapeValue(project)}" Sdk="{EscapeValue(sdk.Name)}" Version="{EscapeValue(sdk.Version)}" />
                    """);
            }
        }
    }

    public static SourceText? RemoveDirectivesFromFile(ImmutableArray<CSharpDirective> directives, SourceText text)
    {
        if (directives.Length == 0)
        {
            return null;
        }

        Debug.Assert(directives.OrderBy(d => d.Info.Span.Start).SequenceEqual(directives), "Directives should be ordered by source location.");

        for (int i = directives.Length - 1; i >= 0; i--)
        {
            var directive = directives[i];
            text = text.Replace(directive.Info.Span, string.Empty);
        }

        return text;
    }

    public static void RemoveDirectivesFromFile(ImmutableArray<CSharpDirective> directives, SourceText text, string filePath)
    {
        if (RemoveDirectivesFromFile(directives, text) is { } modifiedText)
        {
            new SourceFile(filePath, modifiedText).Save();
        }
    }
}
