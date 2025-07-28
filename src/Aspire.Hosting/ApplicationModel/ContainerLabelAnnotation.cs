// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents an annotation for container labels.
/// </summary>
[DebuggerDisplay("Type = {GetType().Name,nq}, Labels = {Labels.Count}")]
public sealed class ContainerLabelAnnotation : IResourceAnnotation, IEnumerable<KeyValuePair<string, string>>
{
    /// <summary>
    /// Gets the labels dictionary.
    /// </summary>
    public Dictionary<string, string> Labels { get; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ContainerLabelAnnotation"/> class.
    /// </summary>
    public ContainerLabelAnnotation()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ContainerLabelAnnotation"/> class with initial labels.
    /// </summary>
    /// <param name="labels">The initial labels to add.</param>
    public ContainerLabelAnnotation(IDictionary<string, string> labels)
    {
        ArgumentNullException.ThrowIfNull(labels);

        foreach (var label in labels)
        {
            Labels[label.Key] = label.Value;
        }
    }

    /// <summary>
    /// Adds a label to the annotation.
    /// </summary>
    /// <param name="key">The label key.</param>
    /// <param name="value">The label value.</param>
    public void Add(string key, string value)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);

        Labels[key] = value;
    }

    /// <summary>
    /// Returns an enumerator that iterates through the labels.
    /// </summary>
    /// <returns>An enumerator for the labels.</returns>
    public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => Labels.GetEnumerator();

    /// <summary>
    /// Returns an enumerator that iterates through the labels.
    /// </summary>
    /// <returns>An enumerator for the labels.</returns>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}