// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel.Docker;

/// <summary>
/// Provides methods for building a stage within a multi-stage container file.
/// </summary>
public interface IContainerStageBuilder
{
    /// <summary>
    /// Gets the name of the stage.
    /// </summary>
    string? StageName { get; }

    /// <summary>
    /// Adds a WORKDIR statement to set the working directory.
    /// </summary>
    /// <param name="path">The working directory path.</param>
    /// <returns>The current stage builder.</returns>
    IContainerStageBuilder WorkDir(string path);

    /// <summary>
    /// Adds a RUN statement to execute a command.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <returns>The current stage builder.</returns>
    IContainerStageBuilder Run(string command);

    /// <summary>
    /// Adds a COPY statement to copy files from the build context.
    /// </summary>
    /// <param name="source">The source path or pattern.</param>
    /// <param name="destination">The destination path.</param>
    /// <returns>The current stage builder.</returns>
    IContainerStageBuilder Copy(string source, string destination);

    /// <summary>
    /// Adds a COPY statement to copy files from another stage.
    /// </summary>
    /// <param name="stage">The source stage name.</param>
    /// <param name="source">The source path in the stage.</param>
    /// <param name="destination">The destination path.</param>
    /// <returns>The current stage builder.</returns>
    IContainerStageBuilder CopyFrom(string stage, string source, string destination);

    /// <summary>
    /// Adds an ENV statement to set an environment variable.
    /// </summary>
    /// <param name="name">The environment variable name.</param>
    /// <param name="value">The environment variable value.</param>
    /// <returns>The current stage builder.</returns>
    IContainerStageBuilder Env(string name, string value);

    /// <summary>
    /// Adds an EXPOSE statement to expose a port.
    /// </summary>
    /// <param name="port">The port number to expose.</param>
    /// <returns>The current stage builder.</returns>
    IContainerStageBuilder Expose(int port);

    /// <summary>
    /// Adds a CMD statement to set the default command.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <returns>The current stage builder.</returns>
    IContainerStageBuilder Cmd(string[] command);

    /// <summary>
    /// Gets the statements for this stage.
    /// </summary>
    IList<IContainerfileStatement> Statements { get; }
}