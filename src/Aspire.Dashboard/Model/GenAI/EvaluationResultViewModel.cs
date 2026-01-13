// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Aspire.Dashboard.Model.GenAI;

[DebuggerDisplay("Name = {Name}, ScoreValue = {ScoreValue}, ScoreLabel = {ScoreLabel}")]
public class EvaluationResultViewModel
{
    public required string Name { get; init; }
    public string? ScoreLabel { get; init; }
    public double? ScoreValue { get; init; }
    public string? Explanation { get; init; }
    public string? ResponseId { get; init; }
    public string? ErrorType { get; init; }
}
