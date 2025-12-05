// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Packaging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Semver;

namespace Aspire.Cli.Tests.Packaging;

public class PackageMigrationTests
{
    private static ILogger<PackageMigration> CreateTestLogger()
    {
        return NullLogger<PackageMigration>.Instance;
    }

    /// <summary>
    /// Helper method to create a version predicate for versions &gt;= threshold
    /// </summary>
    private static Func<SemVersion, bool> VersionAtLeast(string threshold)
    {
        var thresholdVersion = SemVersion.Parse(threshold, SemVersionStyles.Strict);
        return v => SemVersion.ComparePrecedence(v, thresholdVersion) >= 0;
    }

    /// <summary>
    /// Helper method to create a version predicate for versions &lt; threshold
    /// </summary>
    private static Func<SemVersion, bool> VersionBelow(string threshold)
    {
        var thresholdVersion = SemVersion.Parse(threshold, SemVersionStyles.Strict);
        return v => SemVersion.ComparePrecedence(v, thresholdVersion) < 0;
    }

    /// <summary>
    /// Helper method to create a version predicate for versions in a range [min, max)
    /// </summary>
    private static Func<SemVersion, bool> VersionInRange(string minInclusive, string maxExclusive)
    {
        var minVersion = SemVersion.Parse(minInclusive, SemVersionStyles.Strict);
        var maxVersion = SemVersion.Parse(maxExclusive, SemVersionStyles.Strict);
        return v => SemVersion.ComparePrecedence(v, minVersion) >= 0 
                 && SemVersion.ComparePrecedence(v, maxVersion) < 0;
    }

    #region GetMigration - Basic functionality tests

    [Fact]
    public void GetMigration_WhenNoMatchingRule_ReturnsNull()
    {
        // Arrange
        var migration = new PackageMigration(CreateTestLogger());
        var targetVersion = SemVersion.Parse("9.5.0", SemVersionStyles.Strict);

        // Act
        var result = migration.GetMigration(targetVersion, "Aspire.Hosting.NonExistentPackage");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetMigration_WhenPackageIdDoesNotExist_ReturnsNull()
    {
        // Arrange
        var migration = new PackageMigration(CreateTestLogger());
        var targetVersion = SemVersion.Parse("10.0.0", SemVersionStyles.Strict);

        // Act
        var result = migration.GetMigration(targetVersion, "SomeOtherPackage.NotAspire");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetMigration_WithEmptyPackageId_ReturnsNull()
    {
        // Arrange
        var migration = new PackageMigration(CreateTestLogger());
        var targetVersion = SemVersion.Parse("10.0.0", SemVersionStyles.Strict);

        // Act
        var result = migration.GetMigration(targetVersion, string.Empty);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GetMigration - Version matching tests

    [Fact]
    public void GetMigration_WithPrereleaseVersion_EvaluatesPredicateCorrectly()
    {
        // Arrange
        var migration = new PackageMigration(CreateTestLogger());
        var prereleaseVersion = SemVersion.Parse("10.0.0-preview.1", SemVersionStyles.Strict);

        // Act - testing that prerelease versions are handled without throwing
        var result = migration.GetMigration(prereleaseVersion, "Aspire.Hosting.SomePackage");

        // Assert
        Assert.Null(result); // No migration rules defined, so should be null
    }

    [Fact]
    public void GetMigration_WithDailyBuildVersion_EvaluatesPredicateCorrectly()
    {
        // Arrange
        var migration = new PackageMigration(CreateTestLogger());
        var dailyVersion = SemVersion.Parse("10.0.0-daily.12345.1", SemVersionStyles.Strict);

        // Act
        var result = migration.GetMigration(dailyVersion, "Aspire.Hosting.SomePackage");

        // Assert
        Assert.Null(result); // No migration rules defined, so should be null
    }

    #endregion

    #region GetMigration - Case insensitivity tests

    [Fact]
    public void GetMigration_PackageIdComparison_IsCaseInsensitive()
    {
        // Arrange - Using a testable migration service with predefined rules
        var migration = new TestablePackageMigration(CreateTestLogger(), [
            (VersionAtLeast("10.0.0"), "Aspire.Hosting.OldPackage", "Aspire.Hosting.NewPackage")
        ]);
        var targetVersion = SemVersion.Parse("10.0.0", SemVersionStyles.Strict);

        // Act
        var lowerCaseResult = migration.GetMigration(targetVersion, "aspire.hosting.oldpackage");
        var upperCaseResult = migration.GetMigration(targetVersion, "ASPIRE.HOSTING.OLDPACKAGE");
        var mixedCaseResult = migration.GetMigration(targetVersion, "Aspire.Hosting.OldPackage");

        // Assert
        Assert.Equal("Aspire.Hosting.NewPackage", lowerCaseResult);
        Assert.Equal("Aspire.Hosting.NewPackage", upperCaseResult);
        Assert.Equal("Aspire.Hosting.NewPackage", mixedCaseResult);
    }

    #endregion

    #region Upward migration tests (stable -> daily/preview)

    [Fact]
    public void GetMigration_UpwardMigration_WhenTargetVersionMeetsThreshold_ReturnsMigratedPackage()
    {
        // Arrange - Simulating an upgrade scenario
        var migration = new TestablePackageMigration(CreateTestLogger(), [
            (VersionAtLeast("10.0.0"), "Aspire.Hosting.LegacyPackage", "Aspire.Hosting.ModernPackage")
        ]);
        var targetVersion = SemVersion.Parse("10.0.0", SemVersionStyles.Strict);

        // Act
        var result = migration.GetMigration(targetVersion, "Aspire.Hosting.LegacyPackage");

        // Assert
        Assert.Equal("Aspire.Hosting.ModernPackage", result);
    }

    [Fact]
    public void GetMigration_UpwardMigration_WhenTargetVersionExceedsThreshold_ReturnsMigratedPackage()
    {
        // Arrange - Simulating an upgrade scenario with version beyond threshold
        var migration = new TestablePackageMigration(CreateTestLogger(), [
            (VersionAtLeast("10.0.0"), "Aspire.Hosting.LegacyPackage", "Aspire.Hosting.ModernPackage")
        ]);
        var targetVersion = SemVersion.Parse("10.5.0", SemVersionStyles.Strict);

        // Act
        var result = migration.GetMigration(targetVersion, "Aspire.Hosting.LegacyPackage");

        // Assert
        Assert.Equal("Aspire.Hosting.ModernPackage", result);
    }

    [Fact]
    public void GetMigration_UpwardMigration_WhenTargetVersionBelowThreshold_ReturnsNull()
    {
        // Arrange - Simulating scenario where version doesn't meet threshold
        var migration = new TestablePackageMigration(CreateTestLogger(), [
            (VersionAtLeast("10.0.0"), "Aspire.Hosting.LegacyPackage", "Aspire.Hosting.ModernPackage")
        ]);
        var targetVersion = SemVersion.Parse("9.5.0", SemVersionStyles.Strict);

        // Act
        var result = migration.GetMigration(targetVersion, "Aspire.Hosting.LegacyPackage");

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region Downward migration tests (daily/preview -> stable)

    [Fact]
    public void GetMigration_DownwardMigration_WhenTargetVersionBelowThreshold_ReturnsMigratedPackage()
    {
        // Arrange - Simulating a downgrade scenario (daily -> stable)
        var migration = new TestablePackageMigration(CreateTestLogger(), [
            (VersionBelow("10.0.0"), "Aspire.Hosting.NewPackage", "Aspire.Hosting.OldPackage")
        ]);
        var targetVersion = SemVersion.Parse("9.5.0", SemVersionStyles.Strict);

        // Act
        var result = migration.GetMigration(targetVersion, "Aspire.Hosting.NewPackage");

        // Assert
        Assert.Equal("Aspire.Hosting.OldPackage", result);
    }

    [Fact]
    public void GetMigration_DownwardMigration_WhenTargetVersionMeetsThreshold_ReturnsNull()
    {
        // Arrange - Simulating scenario where version doesn't require downgrade migration
        var migration = new TestablePackageMigration(CreateTestLogger(), [
            (VersionBelow("10.0.0"), "Aspire.Hosting.NewPackage", "Aspire.Hosting.OldPackage")
        ]);
        var targetVersion = SemVersion.Parse("10.0.0", SemVersionStyles.Strict);

        // Act
        var result = migration.GetMigration(targetVersion, "Aspire.Hosting.NewPackage");

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region Bidirectional migration tests

    [Fact]
    public void GetMigration_BidirectionalMigrations_UpwardMigrationWorks()
    {
        // Arrange - Setup bidirectional migration rules
        var migration = new TestablePackageMigration(CreateTestLogger(), [
            // Upward migration: when upgrading to 10.0.0+, migrate OldPackage -> NewPackage
            (VersionAtLeast("10.0.0"), "Aspire.Hosting.OldPackage", "Aspire.Hosting.NewPackage"),
            // Downward migration: when downgrading to < 10.0.0, migrate NewPackage -> OldPackage
            (VersionBelow("10.0.0"), "Aspire.Hosting.NewPackage", "Aspire.Hosting.OldPackage")
        ]);

        // Act - Test upward migration
        var upgradeResult = migration.GetMigration(
            SemVersion.Parse("10.0.0", SemVersionStyles.Strict), 
            "Aspire.Hosting.OldPackage");

        // Assert
        Assert.Equal("Aspire.Hosting.NewPackage", upgradeResult);
    }

    [Fact]
    public void GetMigration_BidirectionalMigrations_DownwardMigrationWorks()
    {
        // Arrange - Setup bidirectional migration rules
        var migration = new TestablePackageMigration(CreateTestLogger(), [
            // Upward migration
            (VersionAtLeast("10.0.0"), "Aspire.Hosting.OldPackage", "Aspire.Hosting.NewPackage"),
            // Downward migration
            (VersionBelow("10.0.0"), "Aspire.Hosting.NewPackage", "Aspire.Hosting.OldPackage")
        ]);

        // Act - Test downward migration
        var downgradeResult = migration.GetMigration(
            SemVersion.Parse("9.5.0", SemVersionStyles.Strict), 
            "Aspire.Hosting.NewPackage");

        // Assert
        Assert.Equal("Aspire.Hosting.OldPackage", downgradeResult);
    }

    [Fact]
    public void GetMigration_BidirectionalMigrations_SamePackageNoMigrationNeeded()
    {
        // Arrange - Setup bidirectional migration rules
        var migration = new TestablePackageMigration(CreateTestLogger(), [
            // Upward migration
            (VersionAtLeast("10.0.0"), "Aspire.Hosting.OldPackage", "Aspire.Hosting.NewPackage"),
            // Downward migration
            (VersionBelow("10.0.0"), "Aspire.Hosting.NewPackage", "Aspire.Hosting.OldPackage")
        ]);

        // Act - Old package at old version - no migration needed
        var oldPackageAtOldVersion = migration.GetMigration(
            SemVersion.Parse("9.5.0", SemVersionStyles.Strict), 
            "Aspire.Hosting.OldPackage");

        // Act - New package at new version - no migration needed
        var newPackageAtNewVersion = migration.GetMigration(
            SemVersion.Parse("10.0.0", SemVersionStyles.Strict), 
            "Aspire.Hosting.NewPackage");

        // Assert
        Assert.Null(oldPackageAtOldVersion);
        Assert.Null(newPackageAtNewVersion);
    }

    #endregion

    #region Multiple rule match error tests

    [Fact]
    public void GetMigration_WhenMultipleRulesMatch_ThrowsPackageMigrationException()
    {
        // Arrange - Setup conflicting rules
        var migration = new TestablePackageMigration(CreateTestLogger(), [
            (VersionAtLeast("10.0.0"), "Aspire.Hosting.SomePackage", "Aspire.Hosting.TargetA"),
            (VersionAtLeast("9.5.0"), "Aspire.Hosting.SomePackage", "Aspire.Hosting.TargetB")
        ]);
        var targetVersion = SemVersion.Parse("10.0.0", SemVersionStyles.Strict);

        // Act & Assert
        var exception = Assert.Throws<PackageMigrationException>(() => 
            migration.GetMigration(targetVersion, "Aspire.Hosting.SomePackage"));

        Assert.Contains("Multiple migration rules match", exception.Message);
        Assert.Contains("Aspire.Hosting.SomePackage", exception.Message);
        Assert.Contains("10.0.0", exception.Message);
    }

    [Fact]
    public void GetMigration_WhenMultipleRulesMatch_ExceptionIncludesTargetPackages()
    {
        // Arrange - Setup conflicting rules
        var migration = new TestablePackageMigration(CreateTestLogger(), [
            (_ => true, "Aspire.Hosting.ConflictingPackage", "Aspire.Hosting.Target1"),
            (_ => true, "Aspire.Hosting.ConflictingPackage", "Aspire.Hosting.Target2")
        ]);
        var targetVersion = SemVersion.Parse("10.0.0", SemVersionStyles.Strict);

        // Act & Assert
        var exception = Assert.Throws<PackageMigrationException>(() => 
            migration.GetMigration(targetVersion, "Aspire.Hosting.ConflictingPackage"));

        Assert.Contains("Target1", exception.Message);
        Assert.Contains("Target2", exception.Message);
    }

    #endregion

    #region Complex version predicate tests

    [Fact]
    public void GetMigration_WithVersionRangePredicate_MatchesCorrectly()
    {
        // Arrange - Setup rule that only applies to specific version range [10.0.0, 11.0.0)
        var migration = new TestablePackageMigration(CreateTestLogger(), [
            (VersionInRange("10.0.0", "11.0.0"), "Aspire.Hosting.RangePackage", "Aspire.Hosting.RangeTarget")
        ]);

        // Act
        var belowRange = migration.GetMigration(SemVersion.Parse("9.5.0", SemVersionStyles.Strict), "Aspire.Hosting.RangePackage");
        var inRange = migration.GetMigration(SemVersion.Parse("10.5.0", SemVersionStyles.Strict), "Aspire.Hosting.RangePackage");
        var aboveRange = migration.GetMigration(SemVersion.Parse("11.0.0", SemVersionStyles.Strict), "Aspire.Hosting.RangePackage");

        // Assert
        Assert.Null(belowRange);
        Assert.Equal("Aspire.Hosting.RangeTarget", inRange);
        Assert.Null(aboveRange);
    }

    [Fact]
    public void GetMigration_WithPrereleaseVersionPredicate_MatchesCorrectly()
    {
        // Arrange - Setup rule that includes prerelease versions
        var previewThreshold = SemVersion.Parse("10.0.0-preview.1", SemVersionStyles.Strict);
        var migration = new TestablePackageMigration(CreateTestLogger(), [
            (v => SemVersion.ComparePrecedence(v, previewThreshold) >= 0, 
             "Aspire.Hosting.PreviewPackage", "Aspire.Hosting.PreviewTarget")
        ]);

        // Act
        var stableBelow = migration.GetMigration(SemVersion.Parse("9.5.0", SemVersionStyles.Strict), "Aspire.Hosting.PreviewPackage");
        var previewMatch = migration.GetMigration(SemVersion.Parse("10.0.0-preview.1", SemVersionStyles.Strict), "Aspire.Hosting.PreviewPackage");
        var previewAbove = migration.GetMigration(SemVersion.Parse("10.0.0-preview.5", SemVersionStyles.Strict), "Aspire.Hosting.PreviewPackage");
        var stableAbove = migration.GetMigration(SemVersion.Parse("10.0.0", SemVersionStyles.Strict), "Aspire.Hosting.PreviewPackage");

        // Assert
        Assert.Null(stableBelow);
        Assert.Equal("Aspire.Hosting.PreviewTarget", previewMatch);
        Assert.Equal("Aspire.Hosting.PreviewTarget", previewAbove);
        Assert.Equal("Aspire.Hosting.PreviewTarget", stableAbove);
    }

    #endregion

    #region Multiple packages migration tests

    [Fact]
    public void GetMigration_WithMultiplePackageMigrations_EachPackageResolvesCorrectly()
    {
        // Arrange - Setup rules for multiple packages
        var migration = new TestablePackageMigration(CreateTestLogger(), [
            (VersionAtLeast("10.0.0"), "Aspire.Hosting.PackageA", "Aspire.Hosting.NewPackageA"),
            (VersionAtLeast("10.0.0"), "Aspire.Hosting.PackageB", "Aspire.Hosting.NewPackageB"),
            (VersionAtLeast("10.0.0"), "Aspire.Hosting.PackageC", "Aspire.Hosting.NewPackageC")
        ]);
        var targetVersion = SemVersion.Parse("10.0.0", SemVersionStyles.Strict);

        // Act
        var resultA = migration.GetMigration(targetVersion, "Aspire.Hosting.PackageA");
        var resultB = migration.GetMigration(targetVersion, "Aspire.Hosting.PackageB");
        var resultC = migration.GetMigration(targetVersion, "Aspire.Hosting.PackageC");
        var resultUnknown = migration.GetMigration(targetVersion, "Aspire.Hosting.PackageD");

        // Assert
        Assert.Equal("Aspire.Hosting.NewPackageA", resultA);
        Assert.Equal("Aspire.Hosting.NewPackageB", resultB);
        Assert.Equal("Aspire.Hosting.NewPackageC", resultC);
        Assert.Null(resultUnknown);
    }

    #endregion

    #region Helper class for testing with custom rules

    /// <summary>
    /// A testable version of PackageMigration that allows injecting custom migration rules for testing.
    /// </summary>
    private sealed class TestablePackageMigration : IPackageMigration
    {
        private readonly ILogger<PackageMigration> _logger;
        private readonly List<(Func<SemVersion, bool> VersionPredicate, string FromPackageId, string ToPackageId)> _migrationRules;

        public TestablePackageMigration(
            ILogger<PackageMigration> logger,
            List<(Func<SemVersion, bool> VersionPredicate, string FromPackageId, string ToPackageId)> migrationRules)
        {
            _logger = logger;
            _migrationRules = migrationRules;
        }

        public string? GetMigration(SemVersion targetHostingVersion, string packageId)
        {
            _logger.LogDebug("Checking migration rules for package '{PackageId}' targeting version '{TargetVersion}'", packageId, targetHostingVersion);

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

    #endregion
}
