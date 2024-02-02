// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Hashing;
using System.Text;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Publishing;

namespace Aspire.Hosting.Azure;

public class AzureBicepResource(string name, string? templateFile = null, string? templateString = null, string? templateResouceName = null) : Resource(name), IAzureResource
{
    private string? TemplateFile { get; } = templateFile;

    private string? TemplateString { get; } = templateString;

    private string? TemplateResourceName { get; } = templateResouceName;

    public string? ConnectionStringTemplate { get; set; }

    public BicepTemplateFile GetBicepTemplateFile(string? directory = null, bool deleteTemporaryFilesOnDispose = true)
    {
        var path = TemplateFile;
        var isTempFile = false;

        if (path is null)
        {
            isTempFile = directory is null;

            path = Path.GetTempFileName() + ".bicep";

            if (TemplateResourceName is null)
            {
                // TODO: Make users specify a name for the template
                File.WriteAllText(path, TemplateString);
            }
            else
            {
                path = directory is null
                    ? path
                    : Path.Combine(directory, $"{TemplateResourceName.ToLowerInvariant()}");

                if (!File.Exists(path))
                {
                    using var resourceStream = GetType().Assembly.GetManifestResourceStream(TemplateResourceName)
                        ?? throw new InvalidOperationException($"Could not find resource {TemplateResourceName} in assembly {GetType().Assembly}");

                    using var fs = File.OpenWrite(path);
                    resourceStream.CopyTo(fs);
                }
            }
        }

        return new(path, isTempFile && deleteTemporaryFilesOnDispose);
    }

    public Dictionary<string, object?> Parameters { get; } = new();

    public Dictionary<string, string?> Outputs { get; } = new();

    // TODO: Make the name bicep safe
    public string CreateBicepResourceName() => Name.ToLower();

    public static string EvalParameter(object? input)
    {
        static string Quote(string s) => $"\"{s}\"";
        static string SingleQuote(string s) => $"'{s}'";
        static string Parenthesize(string s) => $"[{s}]";
        static string Join(IEnumerable<string> s) => string.Join(", ", s);

        return input switch
        {
            string s => Quote(s),
            IEnumerable<string> enumerable => Quote(Parenthesize(Join(enumerable.Select(SingleQuote)))),
            IResourceBuilder<IResourceWithConnectionString> builder => Quote(builder.Resource.GetConnectionString() ?? throw new InvalidOperationException("Missing connection string")),
            IResourceBuilder<ParameterResource> p => Quote(p.Resource.Value),
            object o => Quote(input.ToString()!),
            null => ""
        };
    }

    public string GetChecksum()
    {
        // TODO: PERF Inefficient

        // First the parameters
        var combined = string.Join(";", Parameters.OrderBy(p => p.Key).Select(p => $"{p.Key}={EvalParameter(p.Value)}"));

        if (TemplateFile is not null)
        {
            combined += File.ReadAllText(TemplateFile);
        }
        else if (TemplateString is not null)
        {
            combined += TemplateString;
        }
        else if (TemplateResourceName is not null)
        {
            using var stream = GetType().Assembly.GetManifestResourceStream(TemplateResourceName) ??
                throw new InvalidOperationException($"Could not find resource {TemplateResourceName} in assembly {GetType().Assembly}");

            combined += new StreamReader(stream).ReadToEnd();
        }

        var hashedContents = Crc32.Hash(Encoding.UTF8.GetBytes(combined));

        return Convert.ToHexString(hashedContents).ToLower();
    }

    public virtual void WriteToManifest(ManifestPublishingContext context)
    {
        var resource = this;

        context.Writer.WriteString("type", "azure.bicep.v0");

        using var template = resource.GetBicepTemplateFile(Path.GetDirectoryName(context.ManifestPath), deleteTemporaryFilesOnDispose: false);
        var path = template.Path;

        if (resource.ConnectionStringTemplate is not null)
        {
            context.Writer.WriteString("connectionString", resource.ConnectionStringTemplate);
        }

        context.Writer.WriteString("path", context.GetManifestRelativePath(path));

        if (resource.Parameters.Count > 0)
        {
            context.Writer.WriteStartObject("params");
            foreach (var input in resource.Parameters)
            {
                if (input.Value is IEnumerable<string> enumerable)
                {
                    context.Writer.WriteStartArray(input.Key);
                    foreach (var item in enumerable)
                    {
                        context.Writer.WriteStringValue(item);
                    }
                    context.Writer.WriteEndArray();
                    continue;
                }

                var value = input.Value switch
                {
                    IResourceBuilder<ParameterResource> p => $"{{{p.Resource.Name}.value}}",
                    IResourceBuilder<IResourceWithConnectionString> p => $"{{{p.Resource.Name}.connectionString}}",
                    object obj => obj.ToString(),
                    null => ""
                };

                context.Writer.WriteString(input.Key, value);
            }
            context.Writer.WriteEndObject();
        }
    }

    public static class KnownParameters
    {
        public const string Location = "location";
        public const string ResourceGroup = "resourceGroup";
        public const string SubscriptionId = "subscriptionId";
        public const string PrincipalId = "principalId";
        public const string PrincipalName = "principalName";
        public const string PrincipalType = "principalType";
    }
}

public readonly struct BicepTemplateFile(string path, bool deleteOnClose) : IDisposable
{
    public string Path { get; } = path;

    public void Dispose()
    {
        if (deleteOnClose)
        {
            File.Delete(Path);
        }
    }
}

public class BicepOutputReference(string name, AzureBicepResource resource)
{
    public string Name { get; } = name;

    public AzureBicepResource Resource { get; } = resource;

    public string? Value => Resource.Outputs[Name];
}

public static class AzureBicepTemplateResourceExtensions
{
    public static IResourceBuilder<AzureBicepResource> AddBicepTemplate(this IDistributedApplicationBuilder builder, string name, string bicepFile)
    {
        var path = Path.GetFullPath(Path.Combine(builder.AppHostDirectory, bicepFile));
        var resource = new AzureBicepResource(name, templateFile: path, templateString: null);
        return builder.AddResource(resource)
                      .WithManifestPublishingCallback(resource.WriteToManifest);
    }

    public static IResourceBuilder<AzureBicepResource> AddBicepTemplateString(this IDistributedApplicationBuilder builder, string name, string bicepContent)
    {
        var resource = new AzureBicepResource(name, templateFile: null, templateString: bicepContent);
        return builder.AddResource(resource)
                      .WithManifestPublishingCallback(resource.WriteToManifest);
    }

    public static BicepOutputReference GetOutput(this IResourceBuilder<AzureBicepResource> builder, string name)
    {
        return new BicepOutputReference(name, builder.Resource);
    }

    public static IResourceBuilder<T> WithEnvironment<T>(this IResourceBuilder<T> builder, string name, BicepOutputReference bicepOutputReference)
        where T : IResourceWithEnvironment
    {
        return builder.WithEnvironment(ctx =>
        {
            if (ctx.PublisherName == "manifest")
            {
                ctx.EnvironmentVariables[name] = $"{{{bicepOutputReference.Resource.Name}.outputs.{bicepOutputReference.Name}}}";
                return;
            }

            if (!bicepOutputReference.Resource.Outputs.TryGetValue(bicepOutputReference.Name, out var value))
            {
                throw new InvalidOperationException($"No output for {bicepOutputReference.Name}");
            }

            ctx.EnvironmentVariables[name] = value?.ToString() ?? "";
        });
    }

    public static IResourceBuilder<T> AddParameter<T>(this IResourceBuilder<T> builder, string name)
        where T : AzureBicepResource
    {
        builder.Resource.Parameters[name] = null;
        return builder;
    }
    public static IResourceBuilder<T> AddParameter<T>(this IResourceBuilder<T> builder, string name, string value)
        where T : AzureBicepResource
    {
        builder.Resource.Parameters[name] = value;
        return builder;
    }

    public static IResourceBuilder<T> AddParameter<T>(this IResourceBuilder<T> builder, string name, IEnumerable<string> value)
        where T : AzureBicepResource
    {
        builder.Resource.Parameters[name] = value;
        return builder;
    }

    public static IResourceBuilder<T> AddParameter<T>(this IResourceBuilder<T> builder, string name, IResourceBuilder<ParameterResource> value)
        where T : AzureBicepResource
    {
        builder.Resource.Parameters[name] = value;
        return builder;
    }

    public static IResourceBuilder<T> AddParameter<T>(this IResourceBuilder<T> builder, string name, IResourceBuilder<IResourceWithConnectionString> value)
        where T : AzureBicepResource
    {
        builder.Resource.Parameters[name] = value;
        return builder;
    }
}
