// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using Aspire.Dashboard.Components.Controls;
using Aspire.Dashboard.Extensions;
using Aspire.Dashboard.Utils;
using Google.Protobuf.WellKnownTypes;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Aspire.Dashboard.Model;

[DebuggerDisplay("Name = {Name}, ResourceType = {ResourceType}, State = {State}, Properties = {Properties.Count}")]
public sealed class ResourceViewModel
{
    public required string Name { get; init; }
    public required string ResourceType { get; init; }
    public required string DisplayName { get; init; }
    public required string Uid { get; init; }
    public required string? State { get; init; }
    public required string? StateStyle { get; init; }
    public required ReadinessState ReadinessState { get; init; }
    public required DateTime? CreationTimeStamp { get; init; }
    public required DateTime? StartTimeStamp { get; init; }
    public required DateTime? StopTimeStamp { get; init; }
    public required ImmutableArray<EnvironmentVariableViewModel> Environment { get; init; }
    public required ImmutableArray<UrlViewModel> Urls { get; init; }
    public required ImmutableArray<VolumeViewModel> Volumes { get; init; }
    public required FrozenDictionary<string, ResourcePropertyViewModel> Properties { get; init; }
    public required ImmutableArray<CommandViewModel> Commands { get; init; }
    public KnownResourceState? KnownState { get; init; }

    internal bool MatchesFilter(string filter)
    {
        // TODO let ResourceType define the additional data values we include in searches
        return Name.Contains(filter, StringComparisons.UserTextSearch);
    }

    public static string GetResourceName(ResourceViewModel resource, IDictionary<string, ResourceViewModel> allResources)
    {
        var count = 0;
        foreach (var (_, item) in allResources)
        {
            if (item.IsHiddenState())
            {
                continue;
            }

            if (item.DisplayName == resource.DisplayName)
            {
                count++;
                if (count >= 2)
                {
                    // There are multiple resources with the same display name so they're part of a replica set.
                    // Need to use the name which has a unique ID to tell them apart.
                    return resource.Name;
                }
            }
        }

        return resource.DisplayName;
    }
}

public enum ReadinessState
{
    Unknown,
    NotReady,
    Ready
}

[DebuggerDisplay("CommandType = {CommandType}, DisplayName = {DisplayName}")]
public sealed class CommandViewModel
{
    private sealed record IconKey(string IconName, IconVariant IconVariant);
    private static readonly ConcurrentDictionary<IconKey, CustomIcon?> s_iconCache = new();

    public string CommandType { get; }
    public CommandViewModelState State { get; }
    public string DisplayName { get; }
    public string DisplayDescription { get; }
    public string ConfirmationMessage { get; }
    public Value? Parameter { get; }
    public bool IsHighlighted { get; }
    public string IconName { get; }
    public IconVariant IconVariant { get; }

    public CommandViewModel(string commandType, CommandViewModelState state, string displayName, string displayDescription, string confirmationMessage, Value? parameter, bool isHighlighted, string iconName, IconVariant iconVariant)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(commandType);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        CommandType = commandType;
        State = state;
        DisplayName = displayName;
        DisplayDescription = displayDescription;
        ConfirmationMessage = confirmationMessage;
        Parameter = parameter;
        IsHighlighted = isHighlighted;
        IconName = iconName;
        IconVariant = iconVariant;
    }

    public static CustomIcon? ResolveIconName(string iconName, IconVariant? iconVariant)
    {
        // Icons.GetInstance isn't efficient. Cache icon lookup.
        return s_iconCache.GetOrAdd(new IconKey(iconName, iconVariant ?? IconVariant.Regular), static key =>
        {
            try
            {
                return Icons.GetInstance(new IconInfo
                {
                    Name = key.IconName,
                    Variant = key.IconVariant,
                    Size = IconSize.Size16
                });
            }
            catch
            {
                // Icon name couldn't be found.
                return null;
            }
        });
    }
}

public enum CommandViewModelState
{
    Enabled,
    Disabled,
    Hidden
}

[DebuggerDisplay("Name = {Name}, Value = {Value}, FromSpec = {FromSpec}, IsValueMasked = {IsValueMasked}")]
public sealed class EnvironmentVariableViewModel : IPropertyGridItem
{
    public string Name { get; }
    public string? Value { get; }
    public bool FromSpec { get; }

    public bool IsValueMasked { get; set; } = true;

    public bool IsValueSensitive => true;

    public bool MatchesFilter(string filter)
        => Name.Contains(filter, StringComparison.CurrentCultureIgnoreCase) ||
           Value?.Contains(filter, StringComparison.CurrentCultureIgnoreCase) == true;

    public EnvironmentVariableViewModel(string name, string? value, bool fromSpec)
    {
        // Name should always have a value, but somehow an empty/whitespace name can reach this point.
        // Better to allow the dashboard to run with an env var with no name than break when loading resources.
        // https://github.com/dotnet/aspire/issues/5309
        Name = name;
        Value = value;
        FromSpec = fromSpec;
    }
}

[DebuggerDisplay("Name = {Name}, Value = {Value}, IsValueSensitive = {IsValueSensitive}, IsValueMasked = {IsValueMasked}")]
public sealed class ResourcePropertyViewModel : IPropertyGridItem
{
    private readonly Lazy<string> _displayValue;
    private readonly Lazy<string> _tooltip;

    public string Name { get; }
    public Value Value { get; }
    public KnownProperty? KnownProperty { get; }
    public string ToolTip => _tooltip.Value;
    public bool IsValueSensitive { get; }
    public bool IsValueMasked { get; set; }
    internal int Priority { get; }

    string? IPropertyGridItem.Name => KnownProperty?.DisplayName ?? Name;

    string? IPropertyGridItem.Value => _displayValue.Value;

    public ResourcePropertyViewModel(string name, Value value, bool isValueSensitive, KnownProperty? knownProperty, int priority, BrowserTimeProvider timeProvider)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name;
        Value = value;
        IsValueSensitive = isValueSensitive;
        KnownProperty = knownProperty;
        Priority = priority;
        IsValueMasked = isValueSensitive;

        _tooltip = new(() => value.HasStringValue ? value.StringValue : value.ToString());

        _displayValue = new(() =>
        {
            var value = Value is { HasStringValue: true, StringValue: var stringValue }
                ? stringValue
                // Complex values such as arrays and objects will be output as JSON.
                // Consider how complex values are rendered in the future.
                : Value.ToString();

            if (Name == KnownProperties.Container.Id)
            {
                // Container images have a short ID of 12 characters
                if (value.Length > 12)
                {
                    value = value[..12];
                }
            }
            else
            {
                // Dates are returned as ISO 8601 text. Try to parse. If successful, format with the current culture.
                if (DateTime.TryParseExact(value, "o", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                {
                    value = FormatHelpers.FormatDateTime(timeProvider, date, cultureInfo: CultureInfo.CurrentCulture);
                }
            }

            return value;
        });
    }

    public bool MatchesFilter(string filter) =>
        Name.Contains(filter, StringComparison.CurrentCultureIgnoreCase) ||
        ToolTip.Contains(filter, StringComparison.CurrentCultureIgnoreCase);
}

public sealed record KnownProperty(string Key, string DisplayName);

[DebuggerDisplay("Name = {Name}, Url = {Url}, IsInternal = {IsInternal}")]
public sealed class UrlViewModel
{
    public string Name { get; }
    public Uri Url { get; }
    public bool IsInternal { get; }

    public UrlViewModel(string name, Uri url, bool isInternal)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(url);

        Name = name;
        Url = url;
        IsInternal = isInternal;
    }
}

public sealed record class VolumeViewModel(string? Source, string Target, string MountType, bool IsReadOnly) : IPropertyGridItem
{
    string? IPropertyGridItem.Name => Source;

    string? IPropertyGridItem.Value => Target;
}
