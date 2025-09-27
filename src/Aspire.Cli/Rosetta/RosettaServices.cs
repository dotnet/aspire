// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Commands;
using Aspire.Cli.Interaction;
using Rosetta;
using Rosetta.Generators;
using Rosetta.Models;

namespace Aspire.Cli.Rosetta;

internal class RosettaServices
{
    public static async Task<ApplicationModel> CreateApplicationModel(string appPath, IInteractionService interactionService, bool debug = false)
    {
        var packagesJson = PackagesJson.Open(appPath);

        var projectModel = new ProjectModel(appPath);

        await EnsureProjectBuilt(projectModel, interactionService);

        var context = projectModel.CreateDependencyContext();

        var integrations = packagesJson.ResolveIntegrations(context, debug);
        var appModel = ApplicationModel.Create(integrations, appPath);
        return appModel;
    }

    public static async Task EnsureProjectBuilt(ProjectModel projectModel, IInteractionService interactionService)
    {
        var packagesJson = PackagesJson.Open(projectModel.AppPath);

        var projectHash = packagesJson.GetPackagesHash();

        if (projectHash != projectModel.GetProjectHash())
        {
            await interactionService.ShowStatusAsync(
            $":hammer_and_wrench:  Generating project files...",
            () =>
            {
                projectModel.CreateProjectFiles(packagesJson.GetPackages());
                return Task.FromResult(0);
            });

            if (File.Exists(projectHash))
            {
                File.Delete(projectHash);
            }

            if (await projectModel.Restore(interactionService))
            {
                // Store the project hash only if restore succeeded
                projectModel.SaveProjectHash(projectHash);
            }
        }
    }

    public static ICodeGenerator CreateCodegenerator(ApplicationModel appModel, PolyglotCommand.Languages? lang = null)
    {
        var appPath = appModel.AppPath;

        if (lang is null)
        {
            // Detect language from appPath
            lang = Directory.Exists(appPath) && Directory.GetFiles(appPath, "*.ts").Length != 0 ? PolyglotCommand.Languages.TypeScript : PolyglotCommand.Languages.Python;
        }

        return lang switch
        {
            PolyglotCommand.Languages.TypeScript => new JavaScriptCodeGenerator(appModel),
            PolyglotCommand.Languages.Python => new PythonCodeGenerator(appModel),
            _ => throw new ArgumentException($"Unsupported language: {lang}"),
        };
    }
}
