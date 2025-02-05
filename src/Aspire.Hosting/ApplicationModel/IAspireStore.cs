// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents a store for managing files in the Aspire hosting environment that can be reused across runs.
/// </summary>
/// <remarks>
/// The store is created in the ./obj folder of the Application Host.
/// If the ASPIRE__STORE__PATH environment variable is set this will be used instead.
///
/// The store is specific to a <see cref="IDistributedApplicationBuilder"/> instance such that each application can't
/// conflict with others. A <em>.aspire</em> prefix is also used to ensure that the folder can be deleted without impacting
/// unrelated files.
/// </remarks>
public interface IAspireStore
{
    /// <summary>
    /// Gets the base path of this store.
    /// </summary>
    string BasePath { get; }

    /// <summary>
    /// Gets a deterministic file path that is a copy of the content from the provided stream.
    /// The resulting file name will depend on the content of the stream.
    /// </summary>
    /// <param name="filenameTemplate">A file name to base the result on.</param>
    /// <param name="contentStream">A stream containing the content.</param>
    /// <returns>A deterministic file path with the same content as the provided stream.</returns>
    string GetFileNameWithContent(string filenameTemplate, Stream contentStream);

    /// <summary>
    /// Gets a deterministic file path that is a copy of the <paramref name="sourceFilename"/>.
    /// The resulting file name will depend on the content of the file.
    /// </summary>
    /// <param name="filenameTemplate">A file name to base the result on.</param>
    /// <param name="sourceFilename">An existing file.</param>
    /// <returns>A deterministic file path with the same content as <paramref name="sourceFilename"/>.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the source file does not exist.</exception>
    string GetFileNameWithContent(string filenameTemplate, string sourceFilename);
}
