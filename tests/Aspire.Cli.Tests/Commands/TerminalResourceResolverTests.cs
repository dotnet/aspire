// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Nodes;
using Aspire.Cli.Backchannel;
using Aspire.Cli.Commands;
using Aspire.Cli.Resources;
using Aspire.Cli.Tests.TestServices;
using Aspire.Dashboard.Model;
using Microsoft.AspNetCore.InternalTesting;

namespace Aspire.Cli.Tests.Commands;

public class TerminalResourceResolverTests
{
    [Fact]
    public async Task AmbiguousDisplayNameFailsBeforeDiscoveryWithoutPrompting()
    {
        var interaction = new TestInteractionService
        {
            PromptForSelectionCallback = (_, _, _, _) => throw new InvalidOperationException("Unexpected replica prompt.")
        };
        var backchannel = new TestAppHostAuxiliaryBackchannel
        {
            ResourceSnapshots =
            [
                new() { Name = "shell-def456", DisplayName = "shell" },
                new() { Name = "shell-abc123", DisplayName = "shell" }
            ],
            GetTerminalInfoHandler = (_, _) => throw new InvalidOperationException("Unexpected terminal discovery.")
        };

        var (_, replica) = await new TerminalResourceResolver(interaction)
            .ResolveAsync(backchannel, "shell", CancellationToken.None).DefaultTimeout();

        Assert.Null(replica);
        Assert.Equal(
            [string.Format(TerminalCommandStrings.ResourceAmbiguous, "shell", "shell-abc123, shell-def456")],
            interaction.DisplayedErrors);
    }

    [Theory]
    [InlineData("shell", null)]
    [InlineData("shell-abc123", null)]
    [InlineData("SHELL-ABC123", "0")]
    public async Task SingletonResolvesDisplayNameOrInstanceId(string resourceName, string? replicaIndex)
    {
        var interaction = new TestInteractionService();
        var backchannel = new TestAppHostAuxiliaryBackchannel
        {
            ResourceSnapshots =
            [
                new() { Name = "shell-abc123", DisplayName = "shell", Properties = Properties(replicaIndex) }
            ]
        };
        var endpoint = new TerminalReplicaInfo { ReplicaIndex = 0, Label = "shell-0", ConsumerUdsPath = "singleton.sock", IsAlive = true };
        backchannel.GetTerminalInfoHandler = (name, _) =>
        {
            Assert.Equal("shell", name);
            return Task.FromResult(new GetTerminalInfoResponse { IsAvailable = true, Replicas = [endpoint] });
        };

        var (name, replica) = await new TerminalResourceResolver(interaction)
            .ResolveAsync(backchannel, resourceName, CancellationToken.None).DefaultTimeout();

        Assert.Equal("shell", name);
        Assert.Same(endpoint, replica);
        Assert.Empty(interaction.DisplayedErrors);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExactInstanceSelectsItsEndpointRegardlessOfResponseOrder(bool reverse)
    {
        var interaction = new TestInteractionService();
        var first = new TerminalReplicaInfo { ReplicaIndex = 0, Label = "shell-0", ConsumerUdsPath = "first.sock", IsAlive = true };
        var selected = new TerminalReplicaInfo { ReplicaIndex = 1, Label = "shell-1", ConsumerUdsPath = "selected.sock", IsAlive = false };
        var backchannel = new TestAppHostAuxiliaryBackchannel
        {
            ResourceSnapshots =
            [
                new() { Name = "shell-abc123", DisplayName = "shell", Properties = Properties("0") },
                new() { Name = "shell-def456", DisplayName = "shell", Properties = Properties("1") }
            ],
            TerminalInfoResponse = new() { IsAvailable = true, Replicas = reverse ? [selected, first] : [first, selected] }
        };

        var (_, replica) = await new TerminalResourceResolver(interaction)
            .ResolveAsync(backchannel, "shell-def456", CancellationToken.None).DefaultTimeout();

        Assert.Same(selected, replica);
        Assert.Empty(interaction.DisplayedErrors);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-an-index")]
    [InlineData("-1")]
    [InlineData("2147483648")]
    [InlineData("7")]
    public async Task InvalidReplicaMappingFailsInsteadOfSelectingAnotherEndpoint(string? replicaIndex)
    {
        var interaction = new TestInteractionService();
        var backchannel = new TestAppHostAuxiliaryBackchannel
        {
            ResourceSnapshots =
            [
                new() { Name = "shell-def456", DisplayName = "shell", Properties = Properties(replicaIndex) }
            ],
            TerminalInfoResponse = new()
            {
                IsAvailable = true,
                Replicas =
                [
                    new() { ReplicaIndex = 0, Label = "shell-0", ConsumerUdsPath = "first.sock", IsAlive = true },
                    new() { ReplicaIndex = 1, Label = "shell-1", ConsumerUdsPath = "second.sock", IsAlive = true }
                ]
            }
        };

        var (_, replica) = await new TerminalResourceResolver(interaction)
            .ResolveAsync(backchannel, "shell-def456", CancellationToken.None).DefaultTimeout();

        Assert.Null(replica);
        Assert.Equal(
            [string.Format(TerminalCommandStrings.InstanceTerminalUnavailable, "shell-def456")],
            interaction.DisplayedErrors);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("1")]
    [InlineData("true")]
    [InlineData("{}")]
    public async Task NonStringReplicaMetadataFailsExplicitly(string json)
    {
        var interaction = new TestInteractionService();
        var backchannel = new TestAppHostAuxiliaryBackchannel
        {
            ResourceSnapshots =
            [
                new()
                {
                    Name = "shell-def456",
                    DisplayName = "shell",
                    Properties = new() { [KnownProperties.Terminal.ReplicaIndex] = JsonNode.Parse(json) }
                }
            ],
            TerminalInfoResponse = new()
            {
                IsAvailable = true,
                Replicas = [new() { ReplicaIndex = 1, Label = "shell-1", ConsumerUdsPath = "unused", IsAlive = true }]
            }
        };

        var (_, replica) = await new TerminalResourceResolver(interaction)
            .ResolveAsync(backchannel, "shell-def456", CancellationToken.None).DefaultTimeout();

        Assert.Null(replica);
        Assert.Equal(
            [string.Format(TerminalCommandStrings.InstanceTerminalUnavailable, "shell-def456")],
            interaction.DisplayedErrors);
    }

    [Fact]
    public async Task MissingMetadataWithOtherSnapshotsDoesNotSelectTheOnlyReturnedEndpoint()
    {
        var interaction = new TestInteractionService();
        var backchannel = new TestAppHostAuxiliaryBackchannel
        {
            ResourceSnapshots =
            [
                new() { Name = "shell-abc123", DisplayName = "shell" },
                new() { Name = "shell-def456", DisplayName = "shell" }
            ],
            TerminalInfoResponse = new()
            {
                IsAvailable = true,
                Replicas = [new() { ReplicaIndex = 0, Label = "shell-0", ConsumerUdsPath = "unused", IsAlive = true }]
            }
        };

        var (_, replica) = await new TerminalResourceResolver(interaction)
            .ResolveAsync(backchannel, "shell-def456", CancellationToken.None).DefaultTimeout();

        Assert.Null(replica);
        Assert.Equal(
            [string.Format(TerminalCommandStrings.InstanceTerminalUnavailable, "shell-def456")],
            interaction.DisplayedErrors);
    }

    [Fact]
    public async Task DisplayNameWithIncompleteSnapshotsStillRequiresAnInstanceId()
    {
        var interaction = new TestInteractionService();
        var backchannel = new TestAppHostAuxiliaryBackchannel
        {
            ResourceSnapshots =
            [
                new() { Name = "shell-abc123", DisplayName = "shell", Properties = Properties("0") }
            ],
            TerminalInfoResponse = new()
            {
                IsAvailable = true,
                Replicas =
                [
                    new() { ReplicaIndex = 0, Label = "shell-0", ConsumerUdsPath = "first.sock", IsAlive = true },
                    new() { ReplicaIndex = 1, Label = "shell-1", ConsumerUdsPath = "second.sock", IsAlive = true }
                ]
            }
        };

        var (_, replica) = await new TerminalResourceResolver(interaction)
            .ResolveAsync(backchannel, "shell", CancellationToken.None).DefaultTimeout();

        Assert.Null(replica);
        Assert.Equal(
            [string.Format(TerminalCommandStrings.ResourceInstanceRequired, "shell")],
            interaction.DisplayedErrors);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UnknownInstanceFailsBeforeTerminalDiscovery(bool hasOtherInstances)
    {
        var interaction = new TestInteractionService();
        var backchannel = new TestAppHostAuxiliaryBackchannel
        {
            ResourceSnapshots = hasOtherInstances ? [new() { Name = "shell-abc123", DisplayName = "shell" }] : [],
            GetTerminalInfoHandler = (_, _) => throw new InvalidOperationException("Unexpected terminal discovery.")
        };

        var (_, replica) = await new TerminalResourceResolver(interaction)
            .ResolveAsync(backchannel, "shell-unknown", CancellationToken.None).DefaultTimeout();

        Assert.Null(replica);
        Assert.Equal(
            [string.Format(TerminalCommandStrings.ResourceNotFound, "shell-unknown")],
            interaction.DisplayedErrors);
    }

    private static Dictionary<string, JsonNode?> Properties(string? replicaIndex)
    {
        return replicaIndex is null ? [] : new() { [KnownProperties.Terminal.ReplicaIndex] = JsonValue.Create(replicaIndex) };
    }
}
