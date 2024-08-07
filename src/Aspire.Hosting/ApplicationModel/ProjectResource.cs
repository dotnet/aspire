// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents a specified .NET project.
/// </summary>
/// <param name="name">The name of the resource.</param>
public class ProjectResource(string name) : Resource(name), IResourceWithEnvironment, IResourceWithArgs, IResourceWithServiceDiscovery
{
    // Keep track of the config host for each Kestrel endpoint annotation
    internal Dictionary<EndpointAnnotation, string> KestrelEndpointAnnotationHosts { get; } = new();

    // Are there any endpoints coming from Kestrel configuration
    internal bool HasKestrelEndpoints => KestrelEndpointAnnotationHosts.Count > 0;

    // Track the https endpoint that was added as a default, and should be excluded from the port & kestrel environment
    internal EndpointAnnotation? DefaultHttpsEndpoint { get; set; }

    internal bool ShouldInjectEndpointEnvironment(EndpointReference e)
    {
        var endpoint = e.EndpointAnnotation;

        if (endpoint.UriScheme is not ("http" or "https") ||    // Only process http and https endpoints
            endpoint.TargetPortEnvironmentVariable is not null) // Skip if target port env variable was set
        {
            return false;
        }

        // If any filter rejects the endpoint, skip it
        return !Annotations.OfType<EndpointEnvironmentInjectionFilterAnnotation>()
                           .Select(a => a.Filter)
                           .Any(f => !f(endpoint));
    }

    private static readonly string? s_aspireProjectRootEnvVar = Environment.GetEnvironmentVariable("ASPIRE_PROJECT_ROOT");

    /// <summary>
    /// FIXME
    /// </summary>
    /// <param name="originalProjectPath"></param>
    /// <param name="label"></param>
    /// <returns></returns>
#pragma warning disable RS0016
    public static string? FindMatchingProjectPath(string? originalProjectPath, string label = "")
#pragma warning restore RS0016
    {
        if (string.IsNullOrEmpty(s_aspireProjectRootEnvVar) || !Directory.Exists(s_aspireProjectRootEnvVar) || string.IsNullOrEmpty(originalProjectPath) || File.Exists(originalProjectPath))
        {
            //Console.WriteLine($"[{label}] root: {s_aspireProjectRootEnvVar}, originalProjectPath: {originalProjectPath}");
            return originalProjectPath;
        }

        // //Console.WriteLine($"s_aspireProjectRootEnvVar: {root}");
        //Console.WriteLine($">> [{label}] originalProjectPath: {originalProjectPath}");

        string filename = Path.GetFileName(originalProjectPath);

        string relativeTargetPath = Path.GetDirectoryName(originalProjectPath)!;
        //Console.WriteLine($"%% [{label}] starting: relativeTargetPath: {relativeTargetPath}");

        string relativeParentPath = "";
        while (true)
        {
            string parentName = Path.GetFileName(relativeTargetPath);
            if (string.IsNullOrEmpty(parentName))
            {
                //Console.WriteLine($"%% [{label}] No parent found for {relativeTargetPath} for {originalProjectPath}");
                break;
            }
            // prepend the parent name to the relativeParentPath
            relativeParentPath = relativeParentPath.Length == 0 ? parentName : Path.Combine(parentName, relativeParentPath);

            // FIXME: check if this should this be done at the end of the block?
            relativeTargetPath = Path.GetDirectoryName(relativeTargetPath)!;
            //Console.WriteLine($"\t%% [{label}] relativePathToTry: {relativeParentPath}");
            if (relativeParentPath == null)
            {
                break;
            }

            string projectPathToTry = Path.Combine(s_aspireProjectRootEnvVar, relativeParentPath, filename);
            //Console.WriteLine($"\t%% [{label}] projectPathToTry: {projectPathToTry}");

            if (File.Exists(projectPathToTry))
            {
                //Console.WriteLine($"\t\t%% [{label}] Using root: {s_aspireProjectRootEnvVar} => returning {projectPathToTry}");
                return projectPathToTry;
            }
        }

        return originalProjectPath;
    }
}
