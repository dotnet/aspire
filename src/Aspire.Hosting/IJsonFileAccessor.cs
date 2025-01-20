// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Nodes;

namespace Aspire.Hosting;

/// <summary>
/// A class that can read a text file formatted as JSON and deserialize the information to a <see cref="JsonObject"/>.
/// </summary>
public interface IJsonFileAccessor
{
    /// <summary>
    /// Deserializes JSON stored in a text file to an instance of <see cref="JsonObject"/>
    /// </summary>
    /// <returns>An instance of <see cref="JsonObject" /> that represents the JSON serialized in the file.</returns>
    JsonObject ReadFileAsJson();

    /// <summary>
    /// Serializes an instance of <see cref="JsonObject" /> to text and saves it in a text file.
    /// </summary>
    /// <param name="updatedContent">A representation of the JSON to save.</param>
    void SaveJson(JsonObject updatedContent);
}
