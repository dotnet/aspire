// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//using Rosetta;
//using Rosetta.Generators;
//using Rosetta.Models;
//using System.CommandLine;

//Func<ApplicationModel, string?, ICodeGenerator> codegenFactory = (appModel, lang) =>
//{
//    var appPath = appModel.AppPath;

//    if (string.IsNullOrEmpty(lang))
//    {
//        // Detect language from appPath
//        lang = Directory.Exists(appPath) && Directory.GetFiles(appPath, "*.ts").Length != 0 ? "js" : "py";
//    }

//    lang = lang.ToLowerInvariant();

//    return lang.ToLower() switch
//    {
//        "js" or "javascript" or "ts" or "typescript" => new JavaScriptCodeGenerator(appModel),
//        "python" or "py" => new PythonCodeGenerator(appModel),
//        _ => throw new ArgumentException($"Unsupported language: {lang}"),
//    };
//};

//var langOption = new Option<string>("--lang", "-l")
//{
//    DefaultValueFactory = (_) => "js",
//    Description = "The language used for the generated code."
//};

//var appPathArgument = new Argument<DirectoryInfo>("appPath")
//{
//    DefaultValueFactory = (_) => new DirectoryInfo(Directory.GetCurrentDirectory()),
//    Description = "The path to the application folder."
//};

//var packageNameArgs = new Argument<string?>("packageName")
//{
//    Description = "The aspire integration package name to add."
//};

//var packageVersionOption = new Option<string?>("--version", "-v")
//{
//    Description = "The Nuget package version to import."
//};

//var debugOption = new Option<bool>("--debug", "-d")
//{
//    Description = "Enable debug logging to the console.",
//    Recursive = true
//};

//var rootCommand = new RootCommand("Sample app for Rosetta")
//{
//    debugOption
//};

//var runCommand = new Command("run", "Run the application")
//{
//    appPathArgument
//};
//runCommand.SetAction(pr => Run(pr.GetValue(appPathArgument)!, pr.GetValue(debugOption)));
//rootCommand.Add(runCommand);

//var serveCommand = new Command("serve", "Starts the remote host")
//{
//    appPathArgument
//};
//serveCommand.SetAction(pr => Serve(pr.GetValue(appPathArgument)!, pr.GetValue(debugOption)));
//rootCommand.Add(serveCommand);

//var importCommand = new Command("add", "Add an aspire integration package")
//{
//    packageNameArgs,
//    packageVersionOption,
//    appPathArgument
//};
//importCommand.SetAction(pr =>
//    Import(
//        pr.GetValue(packageNameArgs),
//        pr.GetValue(packageVersionOption),
//        pr.GetValue(appPathArgument)!
//    )
//);
//rootCommand.Add(importCommand);

//var newCommand = new Command("new", "Create a new application")
//{
//    langOption,
//    appPathArgument
//};
//newCommand.SetAction(pr =>
//    New(
//        pr.GetValue(langOption)!,
//        pr.GetValue(appPathArgument)!
//    )
//);
//rootCommand.Add(newCommand);

//var restoreCommand = new Command("restore", "Restore dependencies for the application")
//{
//    appPathArgument
//};
//restoreCommand.SetAction(pr =>
//    Restore(pr.GetValue(appPathArgument)!)
//);
//rootCommand.Add(restoreCommand);

//return await new CommandLineConfiguration(rootCommand).InvokeAsync(args);

//void Run(DirectoryInfo appPath, bool debug)
//{
//    Restore(appPath);

//    var projectModel = new ProjectModel(appPath.FullName);

//    var appModel = CreateApplicationModel(appPath);

//    var codegen = codegenFactory(appModel, null);

//    foreach (var hostFile in codegen.GenerateHostFiles())
//    {
//        var hostFilePath = Path.Combine(projectModel.ProjectModelPath, hostFile.Key);
//        File.WriteAllText(hostFilePath, hostFile.Value);
//    }

//    // Executes the RemoteAppHost
//    var process = projectModel.Run();

//    // This runs the ts/py apphost
//    _ = codegen.ExecuteAppHost(appPath.FullName);

//    // TODO: exit gracefully to let DCP stop the containers

//    AppDomain.CurrentDomain.ProcessExit += (_, _) => process.Kill();
//    Console.CancelKeyPress += (_, _) => process.Kill();

//    process.WaitForExit();
//}

//void Serve(DirectoryInfo appPath, bool debug)
//{
//    var projectModel = new ProjectModel(appPath.FullName);

//    EnsureProjectBuilt(projectModel);

//    // Executes the RemoteAppHost
//    var process = projectModel.Run();

//    // TODO: exit gracefully to let DCP stop the containers

//    // AppDomain.CurrentDomain.ProcessExit += (_, _) => { Console.WriteLine("Stopping server..."); process.Close(); };
//    // Console.CancelKeyPress += (_, _) => { Console.WriteLine("Stopping server..."); process.Close(); };

//    process.WaitForExit();
//}

//void Import(string? packageName, string? packageVersion, DirectoryInfo appPath)
//{
//    ArgumentException.ThrowIfNullOrEmpty(packageName);

//    var packagesJson = PackagesJson.Open(appPath.FullName);

//    if (PackagesJson.GetPackageByShortName(packageName) is { } reference)
//    {
//        (packageName, packageVersion) = reference;
//    }

//    ArgumentException.ThrowIfNullOrEmpty(packageVersion);

//    packagesJson.Import(packageName, packageVersion);

//    Restore(appPath);
//}

//Task New(string lang, DirectoryInfo appPath)
//{
//    ArgumentNullException.ThrowIfNull(appPath);

//    Directory.CreateDirectory(appPath.FullName);

//    var appModel = CreateApplicationModel(appPath);

//    var codegen = codegenFactory(appModel, lang);

//    codegen.GenerateDistributedApplication();

//    codegen.GenerateAppHost(appPath.FullName);

//    // TODO: We may need to run `npm install` to install the dependencies

//    Console.WriteLine($"New application created in {Utils.NormalizePath(appPath.FullName)}.");

//    return Task.CompletedTask;
//}

//void Restore(DirectoryInfo appPath) => RestoreForLang(appPath, null);

//void RestoreForLang(DirectoryInfo appPath, string? lang)
//{
//    var appModel = CreateApplicationModel(appPath);
//    var codegen = codegenFactory(appModel, lang);

//    codegen.GenerateDistributedApplication();
//}

//static void EnsureProjectBuilt(ProjectModel projectModel)
//{
//    var packagesJson = PackagesJson.Open(projectModel.AppPath);

//    var projectHash = packagesJson.GetPackagesHash();

//    if (projectHash != projectModel.GetProjectHash())
//    {
//        Console.WriteLine("Project dependencies have changed. Restoring...");
//        projectModel.CreateProjectFiles(packagesJson.GetPackages());
//        projectModel.Restore();
//        projectModel.SaveProjectHash(projectHash);
//    }
//}

//static ApplicationModel CreateApplicationModel(DirectoryInfo appPath, bool debug = false)
//{
//    var packagesJson = PackagesJson.Open(appPath.FullName);

//    var projectModel = new ProjectModel(appPath.FullName);

//    EnsureProjectBuilt(projectModel);

//    var context = projectModel.CreateDependencyContext();

//    var integrations = packagesJson.ResolveIntegrations(context, debug);
//    var appModel = ApplicationModel.Create(integrations, appPath.FullName);
//    return appModel;
//}
