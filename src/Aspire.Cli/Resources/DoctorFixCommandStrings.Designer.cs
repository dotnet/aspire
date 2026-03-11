// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.Resources;

internal static class DoctorFixCommandStrings
{
    private static readonly System.Resources.ResourceManager s_resourceManager = new("Aspire.Cli.Resources.DoctorFixCommandStrings", typeof(DoctorFixCommandStrings).Assembly);

    internal static string Description => s_resourceManager.GetString("Description", System.Globalization.CultureInfo.CurrentUICulture) ?? "Fix identified environment issues.";
    internal static string AllOptionDescription => s_resourceManager.GetString("AllOptionDescription", System.Globalization.CultureInfo.CurrentUICulture) ?? "Fix all identified issues.";
    internal static string CheckingPrerequisites => s_resourceManager.GetString("CheckingPrerequisites", System.Globalization.CultureInfo.CurrentUICulture) ?? "Checking prerequisites...";
    internal static string FixingIssues => s_resourceManager.GetString("FixingIssues", System.Globalization.CultureInfo.CurrentUICulture) ?? "Applying fixes...";
    internal static string EvaluatingCheck => s_resourceManager.GetString("EvaluatingCheck", System.Globalization.CultureInfo.CurrentUICulture) ?? "Evaluating {0}...";
    internal static string ApplyingCategoryFixes => s_resourceManager.GetString("ApplyingCategoryFixes", System.Globalization.CultureInfo.CurrentUICulture) ?? "Applying {0} fixes";
    internal static string NoIssuesFound => s_resourceManager.GetString("NoIssuesFound", System.Globalization.CultureInfo.CurrentUICulture) ?? "No issues found for '{0}'. No fix needed.";
    internal static string NoFixableIssues => s_resourceManager.GetString("NoFixableIssues", System.Globalization.CultureInfo.CurrentUICulture) ?? "No fixable issues were identified.";
    internal static string FixApplied => s_resourceManager.GetString("FixApplied", System.Globalization.CultureInfo.CurrentUICulture) ?? "Fix applied: {0}";
    internal static string FixFailed => s_resourceManager.GetString("FixFailed", System.Globalization.CultureInfo.CurrentUICulture) ?? "Fix failed: {0}";
    internal static string UnknownAction => s_resourceManager.GetString("UnknownAction", System.Globalization.CultureInfo.CurrentUICulture) ?? "Unknown action '{0}' for '{1}'.";
    internal static string FixResultsHeader => s_resourceManager.GetString("FixResultsHeader", System.Globalization.CultureInfo.CurrentUICulture) ?? "Fix Results";
    internal static string SummaryFormat => s_resourceManager.GetString("SummaryFormat", System.Globalization.CultureInfo.CurrentUICulture) ?? "Summary: {0} applied, {1} failed";
    internal static string JsonOptionDescription => s_resourceManager.GetString("JsonOptionDescription", System.Globalization.CultureInfo.CurrentUICulture) ?? "Output format (table or json).";
}
