// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Yaml;

/// <summary>
/// Represents a YAML node that encapsulates an expression. This class is used to handle YAML fragments that utilize dynamic or embedded expressions.
/// </summary>
/// <remarks>
/// Represents a YAML node capable of encapsulating a dynamic expression.
/// </remarks>
public class YamlExpressionNode(string expression) : YamlNode
{
    /// <summary>
    /// Represents a string expression in a YAML expression node.
    /// The property is immutable and provides the value of the expression
    /// encapsulated by the current instance of <see cref="YamlExpressionNode"/>.
    /// </summary>
    public string Expression { get; } = expression;

    /// <summary>
    /// Writes the current <see cref="YamlExpressionNode"/> to the specified <see cref="YamlWriter"/>.
    /// </summary>
    /// <param name="writer">The <see cref="YamlWriter"/> to which the YAML expression will be written. Must not be null.</param>
    public override void WriteTo(YamlWriter writer)
    {
        // We'll just pass them as a string for now:
        writer.WriteValue($"{{{{ {Expression} }}}}");
    }
}
