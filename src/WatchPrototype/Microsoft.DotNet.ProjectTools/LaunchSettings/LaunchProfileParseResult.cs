// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.DotNet.ProjectTools;

public sealed class LaunchProfileParseResult
{
    public string? FailureReason { get; }

    public LaunchProfile? Profile { get; }

    private LaunchProfileParseResult(string? failureReason, LaunchProfile? profile)
    {
        FailureReason = failureReason;
        Profile = profile;
    }

    [MemberNotNullWhen(false, nameof(FailureReason))]
    public bool Successful
        => FailureReason == null;

    public static LaunchProfileParseResult Failure(string reason)
        => new(reason, profile: null);

    public static LaunchProfileParseResult Success(LaunchProfile? model)
        => new(failureReason: null, profile: model);
}
