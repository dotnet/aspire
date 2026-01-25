// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Infrastructure.Tests.Helpers;

/// <summary>
/// Evaluates which test categories should run based on changed files.
/// Reimplements the logic from Evaluate-TestSelection.ps1.
/// </summary>
public sealed class TestSelectionEvaluator
{
    private readonly TestSelectionConfig _config;

    public TestSelectionEvaluator(TestSelectionConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    /// <summary>
    /// Creates an evaluator from a configuration file.
    /// </summary>
    public static TestSelectionEvaluator FromFile(string configPath)
    {
        var config = TestSelectionConfig.LoadFromFile(configPath);
        return new TestSelectionEvaluator(config);
    }

    /// <summary>
    /// Creates an evaluator from a JSON string.
    /// </summary>
    public static TestSelectionEvaluator FromJson(string json)
    {
        var config = TestSelectionConfig.LoadFromJson(json);
        return new TestSelectionEvaluator(config);
    }

    /// <summary>
    /// Evaluates test selection based on the given changed files.
    /// </summary>
    /// <param name="changedFiles">List of changed file paths.</param>
    /// <returns>The test selection result.</returns>
    public TestSelectionResult Evaluate(IEnumerable<string> changedFiles)
    {
        var files = changedFiles.ToList();

        // No changes
        if (files.Count == 0)
        {
            var emptyResult = new TestSelectionResult
            {
                RunAll = false,
                TriggerReason = "no_changes"
            };

            foreach (var categoryName in _config.Categories.Keys)
            {
                emptyResult.Categories[categoryName] = new CategoryResult
                {
                    Enabled = false,
                    Reason = "no files changed"
                };
            }

            return emptyResult;
        }

        // Split files into ignored and active
        var (ignoredFiles, activeFiles) = SplitIgnoredAndActive(files);

        // If all files are ignored, no tests need to run
        if (activeFiles.Count == 0)
        {
            var ignoredResult = new TestSelectionResult
            {
                RunAll = false,
                TriggerReason = "all_ignored",
                IgnoredFiles = ignoredFiles
            };

            foreach (var categoryName in _config.Categories.Keys)
            {
                ignoredResult.Categories[categoryName] = new CategoryResult
                {
                    Enabled = false,
                    Reason = "all files ignored"
                };
            }

            return ignoredResult;
        }

        // Check for triggerAll patterns first
        foreach (var (categoryName, category) in _config.Categories)
        {
            if (category.TriggerAll && category.TriggerPaths.Count > 0)
            {
                foreach (var file in activeFiles)
                {
                    foreach (var pattern in category.TriggerPaths)
                    {
                        if (GlobPatternMatcher.IsMatch(file, pattern))
                        {
                            // TriggerAll matched - run all tests
                            var triggerAllResult = new TestSelectionResult
                            {
                                RunAll = true,
                                TriggerReason = "critical_path",
                                TriggerCategory = categoryName,
                                TriggerPattern = pattern,
                                TriggerFile = file,
                                ChangedFiles = activeFiles,
                                IgnoredFiles = ignoredFiles
                            };

                            foreach (var catName in _config.Categories.Keys)
                            {
                                triggerAllResult.Categories[catName] = new CategoryResult
                                {
                                    Enabled = true,
                                    Reason = $"triggerAll: {categoryName} matched {pattern}"
                                };
                            }

                            return triggerAllResult;
                        }
                    }
                }
            }
        }

        // Evaluate each category
        var categoryResults = new Dictionary<string, CategoryResult>();
        var matchedFiles = new HashSet<string>();
        var allProjects = new List<string>();

        foreach (var categoryName in _config.Categories.Keys)
        {
            categoryResults[categoryName] = new CategoryResult
            {
                Enabled = false,
                Reason = "no matching changes"
            };
        }

        foreach (var file in activeFiles)
        {
            var fileMatched = false;

            foreach (var (categoryName, category) in _config.Categories)
            {
                // Skip triggerAll categories (already handled)
                if (category.TriggerAll)
                {
                    continue;
                }

                var matchesInclude = false;
                var matchedPattern = "";

                // Check include patterns
                foreach (var pattern in category.TriggerPaths)
                {
                    if (GlobPatternMatcher.IsMatch(file, pattern))
                    {
                        matchesInclude = true;
                        matchedPattern = pattern;
                        break;
                    }
                }

                if (matchesInclude)
                {
                    // Check exclude patterns
                    var matchesExclude = category.ExcludePaths.Any(pattern =>
                        GlobPatternMatcher.IsMatch(file, pattern));

                    if (!matchesExclude)
                    {
                        categoryResults[categoryName] = new CategoryResult
                        {
                            Enabled = true,
                            Reason = $"matched: {matchedPattern}"
                        };
                        fileMatched = true;

                        // Collect projects
                        foreach (var project in category.Projects)
                        {
                            if (!allProjects.Contains(project))
                            {
                                allProjects.Add(project);
                            }
                        }
                    }
                }
            }

            if (fileMatched)
            {
                matchedFiles.Add(file);
            }
        }

        // Check for unmatched files (conservative fallback)
        var unmatchedFiles = activeFiles.Where(f => !matchedFiles.Contains(f)).ToList();

        if (unmatchedFiles.Count > 0)
        {
            // Conservative fallback - run all tests
            var fallbackResult = new TestSelectionResult
            {
                RunAll = true,
                TriggerReason = "conservative_fallback",
                UnmatchedFiles = unmatchedFiles,
                ChangedFiles = activeFiles,
                IgnoredFiles = ignoredFiles
            };

            foreach (var categoryName in _config.Categories.Keys)
            {
                fallbackResult.Categories[categoryName] = new CategoryResult
                {
                    Enabled = true,
                    Reason = "conservative fallback: unmatched files"
                };
            }

            return fallbackResult;
        }

        // Apply project mappings to get specific test projects
        var mappedProjects = GetProjectsFromMappings(activeFiles);
        foreach (var project in mappedProjects)
        {
            if (!allProjects.Contains(project))
            {
                allProjects.Add(project);
            }
        }

        return new TestSelectionResult
        {
            RunAll = false,
            TriggerReason = "normal",
            ChangedFiles = activeFiles,
            IgnoredFiles = ignoredFiles,
            Categories = categoryResults,
            Projects = allProjects
        };
    }

    /// <summary>
    /// Splits files into ignored and active lists based on ignore patterns.
    /// </summary>
    private (List<string> Ignored, List<string> Active) SplitIgnoredAndActive(IEnumerable<string> files)
    {
        var ignored = new List<string>();
        var active = new List<string>();

        foreach (var file in files)
        {
            var isIgnored = _config.IgnorePaths.Any(pattern =>
                GlobPatternMatcher.IsMatch(file, pattern));

            if (isIgnored)
            {
                ignored.Add(file);
            }
            else
            {
                active.Add(file);
            }
        }

        return (ignored, active);
    }

    /// <summary>
    /// Gets all test projects for a list of files based on project mappings.
    /// </summary>
    private List<string> GetProjectsFromMappings(IEnumerable<string> files)
    {
        var projects = new HashSet<string>();

        foreach (var file in files)
        {
            foreach (var mapping in _config.ProjectMappings)
            {
                var testProject = GetProjectMappingMatch(file, mapping);
                if (testProject != null)
                {
                    projects.Add(testProject);
                }
            }
        }

        return projects.ToList();
    }

    /// <summary>
    /// Checks if a file matches a project mapping and returns the test project path.
    /// </summary>
    private static string? GetProjectMappingMatch(string filePath, ProjectMapping mapping)
    {
        if (!GlobPatternMatcher.TryMatchSourcePattern(filePath, mapping.SourcePattern, out var capturedName))
        {
            return null;
        }

        // Check exclusions
        if (mapping.Exclude.Any(excludePattern => GlobPatternMatcher.IsMatch(filePath, excludePattern)))
        {
            return null;
        }

        // Substitute {name} in test pattern
        var testProject = mapping.TestPattern.Replace("{name}", capturedName);
        return testProject;
    }
}
