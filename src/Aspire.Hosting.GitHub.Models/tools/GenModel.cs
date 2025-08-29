// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#:property PublishAot=false

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

var token = Environment.GetEnvironmentVariable("GITHUB_TOKEN") ?? throw new InvalidOperationException("GITHUB_TOKEN is not set");
using var ghClient = new GitHubModelClient(token);
var ghModels = await ghClient.GetModelsAsync().ConfigureAwait(false);
var ghGenerator = new GitHubModelClassGenerator();
var ghCode = GitHubModelClassGenerator.GenerateCode("Aspire.Hosting.GitHub", ghModels);
File.WriteAllText(Path.Combine("..", "GitHubModel.Generated.cs"), ghCode);
Console.WriteLine("Generated GitHub model descriptors written to GitHubModel.Generated.cs");
Console.WriteLine("\nGitHub Model data:");
Console.WriteLine(JsonSerializer.Serialize(ghModels, new JsonSerializerOptions
{
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
}));

public sealed class GitHubModel
{
    [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    [JsonPropertyName("registry")] public string Registry { get; set; } = string.Empty;
    [JsonPropertyName("publisher")] public string Publisher { get; set; } = string.Empty;
    [JsonPropertyName("summary")] public string Summary { get; set; } = string.Empty;
    [JsonPropertyName("rate_limit_tier")] public string RateLimitTier { get; set; } = string.Empty;
    [JsonPropertyName("html_url")] public string HtmlUrl { get; set; } = string.Empty;
    [JsonPropertyName("version")] public string Version { get; set; } = string.Empty;
    [JsonPropertyName("capabilities")] public List<string> Capabilities { get; set; } = new();
    [JsonPropertyName("limits")] public GitHubModelLimits? Limits { get; set; }
    [JsonPropertyName("tags")] public List<string> Tags { get; set; } = new();
    [JsonPropertyName("supported_input_modalities")] public List<string> SupportedInputModalities { get; set; } = new();
    [JsonPropertyName("supported_output_modalities")] public List<string> SupportedOutputModalities { get; set; } = new();
}

public sealed class GitHubModelLimits
{
    [JsonPropertyName("max_input_tokens")] public int? MaxInputTokens { get; set; }
    [JsonPropertyName("max_output_tokens")] public int? MaxOutputTokens { get; set; }
}

internal sealed class GitHubModelClient : IDisposable
{
    private readonly HttpClient _http = new();
    private readonly string? _token;
    public GitHubModelClient(string? token) => _token = token;
    public async Task<List<GitHubModel>> GetModelsAsync()
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, "https://models.github.ai/catalog/models");
        req.Headers.TryAddWithoutValidation("Accept", "application/vnd.github+json");
        if (!string.IsNullOrWhiteSpace(_token))
        {
            req.Headers.TryAddWithoutValidation("Authorization", $"Bearer {_token}");
        }
        req.Headers.TryAddWithoutValidation("X-GitHub-Api-Version", "2022-11-28");
        using var resp = await _http.SendAsync(req).ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();
        await using var stream = await resp.Content.ReadAsStreamAsync().ConfigureAwait(false);
        var models = await JsonSerializer.DeserializeAsync<List<GitHubModel>>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }).ConfigureAwait(false);
        return models ?? new();
    }
    public void Dispose() => _http.Dispose();
}

internal sealed class GitHubModelClassGenerator
{
    public static string GenerateCode(string ns, List<GitHubModel> models)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// Licensed to the .NET Foundation under one or more agreements.");
        sb.AppendLine("// The .NET Foundation licenses this file to you under the MIT license.");
        sb.AppendLine(CultureInfo.InvariantCulture, $"namespace {ns};");
        sb.AppendLine();
        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// Generated strongly typed model descriptors for GitHub Models.");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("public partial class GitHubModel");
        sb.AppendLine("{");
        var groups = models.Where(m => !string.IsNullOrEmpty(m.Publisher) && !string.IsNullOrEmpty(m.Name))
                           .GroupBy(m => m.Publisher)
                           .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase);
        foreach (var g in groups)
        {
            var className = ToId(g.Key);
            sb.AppendLine("    /// <summary>");
            sb.AppendLine(CultureInfo.InvariantCulture, $"    /// Models published by {EscapeXml(g.Key)}.");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine(CultureInfo.InvariantCulture, $"    public static class {className}");
            sb.AppendLine("    {");
            foreach (var m in g.OrderBy(m => m.Name, StringComparer.OrdinalIgnoreCase))
            {
                var prop = ToId(m.Name);
                var desc = EscapeXml(Clean(m.Summary ?? $"Descriptor for {m.Name}"));
                sb.AppendLine("        /// <summary>");
                sb.AppendLine(CultureInfo.InvariantCulture, $"        /// {desc}");
                sb.AppendLine("        /// </summary>");
                sb.AppendLine(CultureInfo.InvariantCulture, $"        public static readonly GitHubModel {prop} = new() {{ Id = \"{Esc(m.Id)}\" }};");
                sb.AppendLine();
            }
            sb.AppendLine("    }");
            sb.AppendLine();
        }
        sb.AppendLine("}");
        return sb.ToString();
    }
    private static string ToId(string value)
    {
        // First, remove or replace invalid characters with spaces, but preserve + as Plus
        var cleaned = value.Replace('-', ' ').Replace('.', ' ').Replace('_', ' ')
                           .Replace("+", " Plus ") // Preserve + as "Plus" to avoid clashes
                           .Replace('(', ' ').Replace(')', ' ').Replace('[', ' ').Replace(']', ' ')
                           .Replace('{', ' ').Replace('}', ' ').Replace('/', ' ').Replace('\\', ' ')
                           .Replace(':', ' ').Replace(';', ' ').Replace(',', ' ').Replace('|', ' ')
                           .Replace('&', ' ').Replace('%', ' ').Replace('$', ' ').Replace('#', ' ')
                           .Replace('@', ' ').Replace('!', ' ').Replace('?', ' ').Replace('<', ' ')
                           .Replace('>', ' ').Replace('=', ' ').Replace('~', ' ')
                           .Replace('`', ' ').Replace('^', ' ').Replace('*', ' ');

        var parts = cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var sb = new StringBuilder();
        foreach (var p in parts)
        {
            if (p.Length == 0)
            {
                continue;
            }
            if (char.IsDigit(p[0]))
            {
                sb.Append(p);
                continue;
            }
            // Preserve original casing; only capitalize a leading lowercase letter for each token.
            if (char.IsLower(p[0]))
            {
                sb.Append(char.ToUpperInvariant(p[0]));
                if (p.Length > 1)
                {
                    sb.Append(p.AsSpan(1));
                }
            }
            else
            {
                sb.Append(p);
            }
        }
        var result = sb.ToString();

        // Ensure we have a valid identifier (start with letter or underscore)
        if (result.Length == 0 || char.IsDigit(result[0]))
        {
            result = "_" + result;
        }

        return result;
    }
    private static string Clean(string s) => s.Replace('\n', ' ').Replace('\r', ' ').Replace("  ", " ").Trim();
    private static string Esc(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    private static string EscapeXml(string s) => s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;").Replace("'", "&apos;");
}
