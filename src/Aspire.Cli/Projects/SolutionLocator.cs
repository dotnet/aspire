// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Interaction;
using Aspire.Cli.Resources;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Projects;

internal interface ISolutionLocator
{
    Task<FileInfo?> FindSolutionFileAsync(DirectoryInfo startDirectory, CancellationToken cancellationToken = default);
}

internal sealed class SolutionLocator(ILogger<SolutionLocator> logger, IInteractionService interactionService) : ISolutionLocator
{
    public async Task<FileInfo?> FindSolutionFileAsync(DirectoryInfo startDirectory, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Searching for solution files starting from {StartDirectory}", startDirectory.FullName);

        var currentDirectory = startDirectory;
        var allSolutionFiles = new List<FileInfo>();

        // Walk up the directory tree looking for solution files
        while (currentDirectory is not null)
        {
            var solutionFiles = GetSolutionFilesInDirectory(currentDirectory);
            if (solutionFiles.Any())
            {
                logger.LogDebug("Found {Count} solution file(s) in {Directory}", solutionFiles.Count, currentDirectory.FullName);
                
                if (solutionFiles.Count == 1)
                {
                    // Single solution found, use it
                    return solutionFiles[0];
                }
                else
                {
                    // Multiple solutions found, prompt user to choose
                    var selectedSolution = await interactionService.PromptForSelectionAsync(
                        InitCommandStrings.MultipleSolutionsFound,
                        solutionFiles,
                        solutionFile => $"{solutionFile.Name} ({Path.GetRelativePath(startDirectory.FullName, solutionFile.FullName)})",
                        cancellationToken);
                    
                    return selectedSolution;
                }
            }

            currentDirectory = currentDirectory.Parent;
        }

        logger.LogDebug("No solution files found in directory hierarchy");
        return null;
    }

    private static List<FileInfo> GetSolutionFilesInDirectory(DirectoryInfo directory)
    {
        var solutionFiles = new List<FileInfo>();
        
        // Look for .sln files first, then .slnx files
        solutionFiles.AddRange(directory.GetFiles("*.sln", SearchOption.TopDirectoryOnly));
        solutionFiles.AddRange(directory.GetFiles("*.slnx", SearchOption.TopDirectoryOnly));
        
        return solutionFiles;
    }
}