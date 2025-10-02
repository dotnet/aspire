// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.Rosetta.Generators;

public interface ICodeGenerator
{
    /// <summary>
    /// Generates the code depending on imported packages. This is invoked every time 
    /// a restore is performed.
    /// </summary>
    IReadOnlyList<string> GenerateDistributedApplication();

    /// <summary>
    /// Generates the app host files. This is called on 'rosetta new' only so this doesn't
    /// overwrite custom changes.
    /// </summary>
    void GenerateAppHost(string appPath);

    /// <summary>
    /// Executes the app host in the target language and environment. This is called on 'rosetta run' only.
    /// </summary>
    string ExecuteAppHost(string appPath);

    /// <summary>
    /// Generates supporting files for the application required by code generation.
    /// </summary>
    IEnumerable<KeyValuePair<string, string>> GenerateHostFiles();
}
