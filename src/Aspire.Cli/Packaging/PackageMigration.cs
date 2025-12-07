// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;
using Semver;

namespace Aspire.Cli.Packaging;

/// <summary>
/// Provides package migration information for upgrading or downgrading packages
/// when switching between different Aspire versions.
/// </summary>
internal interface IPackageMigration
{
    /// <summary>
    /// Gets the replacement package ID for a package when migrating to a specific target Aspire hosting version.
    /// </summary>
    /// <param name="targetHostingVersion">The target Aspire hosting SDK version being migrated to.</param>
    /// <param name="packageId">The package ID to check for migration.</param>
    /// <returns>
    /// The replacement package ID if a migration rule exists for the given package and version;
    /// <c>null</c> if no migration is needed for this package.
    /// </returns>
    /// <exception cref="PackageMigrationException">Thrown when multiple migration rules match for the same package.</exception>
    string? GetMigration(SemVersion targetHostingVersion, string packageId);
}

/// <summary>
/// Implementation of <see cref="IPackageMigration"/> that provides package migration rules
/// for migrating packages between different Aspire versions.
/// </summary>
internal sealed class PackageMigration : IPackageMigration
{
    private readonly ILogger<PackageMigration> _logger;
    private readonly List<(Func<SemVersion, bool> VersionPredicate, string FromPackageId, string ToPackageId)> _migrationRules;

    /// <summary>
    /// Initializes a new instance of the <see cref="PackageMigration"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public PackageMigration(ILogger<PackageMigration> logger)
    {
        _logger = logger;
        _migrationRules = CreateMigrationRules();
    }

    /// <summary>
    /// Creates the list of migration rules.
    /// Each rule contains a version predicate, the source package ID, and the target package ID.
    /// Rules are evaluated based on the target hosting version.
    /// </summary>
    private static List<(Func<SemVersion, bool> VersionPredicate, string FromPackageId, string ToPackageId)> CreateMigrationRules()
    {
        // Migration rules are defined here. Each rule specifies:
        // - A predicate that determines if the rule applies based on the target version
        // - The package ID being migrated from
        // - The package ID being migrated to
        //
        // Both upwards (stable -> daily) and downwards (daily -> stable) migrations should be defined
        // to support users switching between channels.
        //
        // Example migration rules (placeholder - replace with actual migrations):
        // - When upgrading to version >= 10.0.0, migrate Aspire.Hosting.OldPackage to Aspire.Hosting.NewPackage
        // - When downgrading to version < 10.0.0, migrate Aspire.Hosting.NewPackage to Aspire.Hosting.OldPackage

        var version13 = SemVersion.Parse("13.0.0", SemVersionStyles.Strict);

        return
        [
            // Aspire.Hosting.NodeJs was renamed to Aspire.Hosting.JavaScript in 13.0.0
            (v => v.ComparePrecedenceTo(version13) >= 0, "Aspire.Hosting.NodeJs", "Aspire.Hosting.JavaScript"),
            (v => v.ComparePrecedenceTo(version13) >= 0, "CommunityToolkit.Aspire.Hosting.NodeJs.Extensions", "CommunityToolkit.Aspire.Hosting.JavaScript.Extensions"),
            (v => v.ComparePrecedenceTo(version13) < 0, "Aspire.Hosting.JavaScript", "Aspire.Hosting.NodeJs"),
            (v => v.ComparePrecedenceTo(version13) < 0, "CommunityToolkit.Aspire.Hosting.JavaScript.Extensions", "CommunityToolkit.Aspire.Hosting.NodeJs.Extensions")
        ];
    }

    /// <inheritdoc />
    public string? GetMigration(SemVersion targetHostingVersion, string packageId)
    {
        _logger.LogDebug("Checking migration rules for package '{PackageId}' targeting version '{TargetVersion}'", packageId, targetHostingVersion);

        // Find all matching rules for the given package ID and version
        var matchingRules = _migrationRules
            .Where(rule => string.Equals(rule.FromPackageId, packageId, StringComparison.OrdinalIgnoreCase)
                && rule.VersionPredicate(targetHostingVersion))
            .ToList();

        if (matchingRules.Count == 0)
        {
            _logger.LogDebug("No migration rules found for package '{PackageId}'", packageId);
            return null;
        }

        if (matchingRules.Count > 1)
        {
            var toPackages = string.Join(", ", matchingRules.Select(r => r.ToPackageId));
            _logger.LogError(
                "Multiple migration rules found for package '{PackageId}' at version '{TargetVersion}'. Target packages: {ToPackages}",
                packageId, targetHostingVersion, toPackages);

            throw new PackageMigrationException(
                $"Multiple migration rules match for package '{packageId}' at version '{targetHostingVersion}'. " +
                $"This is a configuration error. Matching target packages: {toPackages}");
        }

        var matchingRule = matchingRules[0];
        _logger.LogInformation(
            "Migration rule found: '{FromPackageId}' -> '{ToPackageId}' for target version '{TargetVersion}'",
            matchingRule.FromPackageId, matchingRule.ToPackageId, targetHostingVersion);

        return matchingRule.ToPackageId;
    }
}

/// <summary>
/// Exception thrown when there is an error in package migration processing.
/// </summary>
internal sealed class PackageMigrationException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PackageMigrationException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public PackageMigrationException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PackageMigrationException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public PackageMigrationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
