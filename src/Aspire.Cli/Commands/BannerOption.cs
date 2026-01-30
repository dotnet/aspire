// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.CommandLine.Invocation;
using Aspire.Cli.Interaction;
using Aspire.Cli.Resources;

namespace Aspire.Cli.Commands;

/// <summary>
/// A command-line option that displays the Aspire CLI animated banner.
/// </summary>
internal sealed class BannerOption : Option<bool>
{
    private CommandLineAction? _action;
    private readonly Func<IBannerService> _bannerServiceFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="BannerOption"/> class.
    /// </summary>
    /// <param name="bannerServiceFactory">Factory to get the banner service.</param>
    public BannerOption(Func<IBannerService> bannerServiceFactory) : base("--banner")
    {
        ArgumentNullException.ThrowIfNull(bannerServiceFactory);

        _bannerServiceFactory = bannerServiceFactory;
        Description = RootCommandStrings.BannerArgumentDescription;
        Arity = ArgumentArity.Zero;
    }

    /// <inheritdoc />
    public override CommandLineAction? Action
    {
        get => _action ??= new BannerOptionAction(_bannerServiceFactory);
        set => _action = value ?? throw new ArgumentNullException(nameof(value));
    }

    private sealed class BannerOptionAction(Func<IBannerService> bannerServiceFactory) : AsynchronousCommandLineAction
    {
        private readonly Func<IBannerService> _bannerServiceFactory = bannerServiceFactory;

        public override async Task<int> InvokeAsync(ParseResult parseResult, CancellationToken cancellationToken)
        {
            var bannerService = _bannerServiceFactory();
            await bannerService.DisplayBannerAsync(cancellationToken);
            return 0;
        }
    }
}
