// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;
using System.Globalization;
using Aspire.Cli.Resources;

namespace Aspire.Cli.Utils.EnvironmentChecker;

/// <summary>
/// Represents a named action that a healable environment check can perform.
/// </summary>
/// <param name="Name">The action name used as a CLI sub-command (e.g., "clean", "trust").</param>
/// <param name="Description">A human-readable description of what the action does, shown in CLI help text.</param>
/// <param name="ProgressDescription">A human-readable description shown as a status message while the action is executing (e.g., "Cleaning development certificates (may require elevated permissions)...").</param>
/// <param name="HealCallback">The callback that executes the heal action.</param>
internal sealed record HealAction(string Name, string Description, string ProgressDescription, Func<CancellationToken, Task<HealResult>> HealCallback);

/// <summary>
/// A keyed collection of <see cref="HealAction"/> instances, indexed by <see cref="HealAction.Name"/>.
/// </summary>
internal sealed class HealActionCollection : KeyedCollection<string, HealAction>
{
    protected override string GetKeyForItem(HealAction item) => item.Name;
}

/// <summary>
/// Represents the result of executing a heal action.
/// </summary>
/// <param name="Success">Whether the heal action completed successfully.</param>
/// <param name="Message">A human-readable message describing the outcome.</param>
/// <param name="Details">Optional additional details about the outcome.</param>
internal sealed record HealResult(bool Success, string Message, string? Details = null);

/// <summary>
/// Interface for environment checks that support automated repair of detected issues.
/// Implementations drive the available sub-commands under <c>aspire doctor fix</c>.
/// </summary>
internal interface IHealableEnvironmentCheck : IEnvironmentCheck
{
    /// <summary>
    /// Gets the name used as a CLI sub-command for fixing this check (e.g., "certificates").
    /// </summary>
    string HealCommandName { get; }

    /// <summary>
    /// Gets the description displayed in CLI help text for this check's fix sub-command.
    /// </summary>
    string HealCommandDescription { get; }

    /// <summary>
    /// Gets the named actions available for this check. Each action becomes a nested
    /// sub-command under the check's fix command. Keyed by <see cref="HealAction.Name"/>.
    /// </summary>
    HealActionCollection HealActions { get; }

    /// <summary>
    /// Evaluates the current environment state and returns the specific heal actions
    /// that should be run to fix detected issues. Returns an empty list when no
    /// fixable issues are found or when detected issues cannot be automatically repaired.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The recommended <see cref="HealAction"/> instances to run, drawn from <see cref="HealActions"/>.</returns>
    Task<IReadOnlyList<HealAction>> EvaluateAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Executes a named heal action by looking it up in <see cref="HealActions"/> and
    /// invoking its <see cref="HealAction.HealCallback"/>.
    /// </summary>
    Task<HealResult> HealAsync(string actionName, CancellationToken cancellationToken)
    {
        if (!HealActions.TryGetValue(actionName, out var healAction))
        {
            return Task.FromResult(new HealResult(false, string.Format(CultureInfo.CurrentCulture, DoctorCommandStrings.HealUnknownAction, actionName)));
        }

        return healAction.HealCallback(cancellationToken);
    }
}
