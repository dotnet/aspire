// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Yaml;

/// <summary>
/// Represents a collection of YAML values, inheriting from a list structure and providing
/// additional functionality specific to handling YAML data.
/// </summary>
/// <typeparam name="T">
/// The type of elements contained in the collection, constrained to reference types.
/// </typeparam>
public class YamlValueCollection<T>(
    IEnumerable<T> collection
) : List<T>(collection), IYamlValueCollection<T>
    where T : class;
