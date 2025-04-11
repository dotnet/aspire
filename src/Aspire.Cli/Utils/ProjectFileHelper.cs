// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine.Parsing;
using System.Diagnostics;

namespace Aspire.Cli.Utils;

internal static class ProjectFileHelper
{
    internal static FileInfo? UseOrFindAppHostProjectFile(FileInfo? projectFile)
    {
        if (projectFile is not null)
        {
            // If the project file is passed, just use it.
            return projectFile;
        }

        var projectFilePaths = Directory.GetFiles(Environment.CurrentDirectory, "*.csproj");
        try 
        {
            var projectFilePath = projectFilePaths?.SingleOrDefault();
            if (projectFilePath is null)
            {
                throw new InvalidOperationException("No project file found.");
            }
            else
            {
                return new FileInfo(projectFilePath);
            }
            
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
            if (projectFilePaths.Length > 1)
            {
                InteractionUtils.DisplayError("The --project option was not specified and multiple *.csproj files were detected.");
                
            }
            else
            {
                InteractionUtils.DisplayError("The --project option was not specified and no *.csproj files were detected.");
            }
            return null;
        };
    }

    internal static void ValidateProjectArgument(ArgumentResult result)
    {
        var value = result.GetValueOrDefault<FileInfo?>();

        if (value is null)
        {
            // Having no value here is fine, but there has to
            // be a single csproj file in the current
            // working directory.
            var csprojFiles = Directory.GetFiles(Environment.CurrentDirectory, "*.csproj");

            if (csprojFiles.Length > 1)
            {
                result.AddError("The project argument was not specified and multiple *.csproj files were detected.");
                return;
            }
            else if (csprojFiles.Length == 0)
            {
                result.AddError("The project argument was not specified and no *.csproj files were detected.");
                return;
            }

            return;
        }

        if (!File.Exists(value.FullName))
        {
            result.AddError("The specified project file does not exist.");
            return;
        }
    }

    internal static void ValidateProjectOption(OptionResult result)
    {
        var value = result.GetValueOrDefault<FileInfo?>();

        if (result.Implicit)
        {
            // Having no value here is fine, but there has to
            // be a single csproj file in the current
            // working directory.
            var csprojFiles = Directory.GetFiles(Environment.CurrentDirectory, "*.csproj");

            if (csprojFiles.Length > 1)
            {
                result.AddError("The --project option was not specified and multiple *.csproj files were detected.");
                return;
            }
            else if (csprojFiles.Length == 0)
            {
                result.AddError("The --project option was not specified and no *.csproj files were detected.");
                return;
            }

            return;
        }

        if (value is null)
        {
            result.AddError("The --project option was specified but no path was provided.");
            return;
        }

        if (!File.Exists(value.FullName))
        {
            result.AddError("The specified project file does not exist.");
            return;
        }
    }
}