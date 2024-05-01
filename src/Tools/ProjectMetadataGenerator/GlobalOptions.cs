// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;

namespace ProjectMetadataGenerator;

internal static class GlobalOptions
{
    public static readonly CliOption<string> s_projectPathOption = new("--project-path")
    {
        Required = true,
        Recursive = true,
        Description = "Path to project file."
    };

    public static readonly CliOption<string> s_metadataTypeNameOption = new("--metadata-type-name")
    {
        Required = true,
        Recursive = true,
        Description = "Generated or assigned metadata type name."
    };

    public static readonly CliOption<string> s_assemblyPathOption = new("--assembly-path")
    {
        Required = true,
        Recursive = true,
        Description = "Path to assembly.",
    };

    public static readonly CliOption<string> s_outputPathOption = new("--output-path")
    {
        Required = true,
        Recursive = true,
        Description = "Output path for generated metadata."
    };

    public static readonly CliOption<bool> s_isExecutableOption = new("--is-executable")
    {
        Recursive = true,
        Description = "Is project an executable."
    };
}
