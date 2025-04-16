// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine.Parsing;
using Aspire.Cli.Projects;

namespace Aspire.Cli.Utils;

internal static class ProjectFileHelper
{
    internal static void ValidateProjectOption(OptionResult result, IProjectLocator projectLocator)
    {
        try
        {
            var value = result.GetValueOrDefault<FileInfo?>();
            projectLocator.UseOrFindAppHostProjectFile(value);
        }
        catch (ProjectLocatorException ex) when (ex.Message == "Project file does not exist.")
        {
            result.AddError("The --project option specified a project that does not exist.");
        }
        catch (ProjectLocatorException ex) when (ex.Message.Contains("Nultiple project files"))
        {
            result.AddError("The --project option was not specified and multiple *.csproj files were detected.");
        }
        catch (ProjectLocatorException ex) when (ex.Message.Contains("No project file"))
        {
            result.AddError("The project argument was not specified and no *.csproj files were detected.");
        }
    }
}