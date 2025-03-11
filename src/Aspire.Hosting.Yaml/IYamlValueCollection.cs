// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;

namespace Aspire.Hosting.Yaml;

/// <summary>
/// Defines a collection of YAML values, supporting enumeration capabilities and
/// serving as a base for more specific YAML value collection implementations.
/// </summary>
public interface IYamlValueCollection : IEnumerable;

/// <summary>
/// Represents a collection of YAML values, providing enumeration and collection manipulation capabilities.
/// </summary>
public interface IYamlValueCollection<T> : IYamlValueCollection, ICollection<T> where T : class;
