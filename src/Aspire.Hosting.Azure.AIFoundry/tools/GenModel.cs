// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#:property PublishAot false

using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

using var mc = new ModelClient();
var allModelsResponse = await mc.GetAllModelsAsync().ConfigureAwait(false);

// Generate C# extension methods for the models
var extensionGenerator = new ModelClassGenerator();
var generatedCode = extensionGenerator.GenerateCode("Aspire.Hosting.Azure", allModelsResponse.Entities ?? []);

// Write the generated code to a file
File.WriteAllText(Path.Combine("..", "AIFoundryModel.Generated.cs"), generatedCode);
Console.WriteLine("Generated extension methods written to AIFoundryModel.Generated.cs");

// Also serialize the strongly typed response for output with pretty printing
var options = new JsonSerializerOptions
{
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
};

Console.WriteLine("\nModel data:");
Console.WriteLine(JsonSerializer.Serialize(allModelsResponse, options));

public class ModelClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly HttpClientHandler _handler;

    public ModelClient()
    {
        _handler = new HttpClientHandler()
        {
            UseCookies = true,
            CookieContainer = new CookieContainer(),
            AutomaticDecompression = DecompressionMethods.All
        };

        _httpClient = new HttpClient(_handler);
    }
    public async Task<ConsolidatedResponse> GetAllModelsAsync()
    {
        var allModels = new List<ModelEntity>();
        string? continuationToken = null;
        string? previousToken;

        int pageCount = 0;

        do
        {
            pageCount++;
            Console.WriteLine($"\n=== Fetching page {pageCount} ===");

            var pageResponse = await GetModelsAsync(continuationToken).ConfigureAwait(false);

            // Deserialize the response using our models
            var apiResponse = JsonSerializer.Deserialize<ApiResponse>(pageResponse);

            if (apiResponse?.IndexEntitiesResponse?.Value != null)
            {
                foreach (var model in apiResponse.IndexEntitiesResponse.Value)
                {
                    allModels.Add(model);
                }

                Console.WriteLine($"Fetched page with {apiResponse.IndexEntitiesResponse.Value.Count} models. Total so far: {allModels.Count}");
            }
            else
            {
                Console.WriteLine("No models found in this page response.");
            }

            // Check if there's a continuation token for the next page
            previousToken = continuationToken;
            continuationToken = apiResponse?.IndexEntitiesResponse?.ContinuationToken;

            if (!string.IsNullOrEmpty(continuationToken))
            {
                Console.WriteLine($"Found continuation token for next page: {continuationToken.Substring(0, Math.Min(50, continuationToken.Length))}...");

                // Check for infinite loop (same token repeated)
                if (continuationToken == previousToken)
                {
                    Console.WriteLine("WARNING: Same continuation token received twice. Breaking to prevent infinite loop.");
                    continuationToken = null;
                }
            }

        } while (!string.IsNullOrEmpty(continuationToken));

        Console.WriteLine($"\n=== Pagination Complete ===");
        Console.WriteLine($"Total pages fetched: {pageCount}");
        Console.WriteLine($"Total models collected: {allModels.Count}");

        // Return the consolidated response using our model
        return new ConsolidatedResponse
        {
            TotalCount = allModels.Count,
            PagesCombined = "All pages fetched",
            Entities = allModels
        };
    }

    public async Task<string> GetModelsAsync(string? continuationToken = null)
    {
        var url = "https://ai.azure.com/api/westus2/ux/v1.0/entities/crossRegion";

        var request = new HttpRequestMessage(HttpMethod.Post, url);

        request.Headers.Add("x-ms-user-agent", "AzureMachineLearningWorkspacePortal/3.0");

        // Build the JSON payload with optional continuation token
        var basePayload = """
        {
            "resourceIds": [
                {"resourceId": "azure-openai", "entityContainerType": "Registry"},
                {"resourceId": "azureml-openai-oss", "entityContainerType": "Registry"},
                {"resourceId": "azureml-msr", "entityContainerType": "Registry"},
                {"resourceId": "azureml", "entityContainerType": "Registry"},
                {"resourceId": "azureml-routers", "entityContainerType": "Registry"},
                {"resourceId": "azureml-phi-prod", "entityContainerType": "Registry"},
                {"resourceId": "azureml-cogsvc", "entityContainerType": "Registry"},
                {"resourceId": "azureml-meta", "entityContainerType": "Registry"},
                {"resourceId": "azureml-mistral", "entityContainerType": "Registry"},
                {"resourceId": "azureml-gretel", "entityContainerType": "Registry"},
                {"resourceId": "nvidia-ai", "entityContainerType": "Registry"},
                {"resourceId": "azureml-nvidia", "entityContainerType": "Registry"},
                {"resourceId": "azureml-ai21", "entityContainerType": "Registry"},
                {"resourceId": "azureml-nixtla", "entityContainerType": "Registry"},
                {"resourceId": "azureml-core42", "entityContainerType": "Registry"},
                {"resourceId": "azureml-cohere", "entityContainerType": "Registry"},
                {"resourceId": "azureml-restricted", "entityContainerType": "Registry"},
                {"resourceId": "HuggingFace", "entityContainerType": "Registry"},
                {"resourceId": "azureml-paige", "entityContainerType": "Registry"},
                {"resourceId": "azureml-bria", "entityContainerType": "Registry"},
                {"resourceId": "azureml-nttdata", "entityContainerType": "Registry"},
                {"resourceId": "azureml-saifr", "entityContainerType": "Registry"},
                {"resourceId": "azureml-saifr-ipp", "entityContainerType": "Registry"},
                {"resourceId": "azureml-saifr-prod", "entityContainerType": "Registry"},
                {"resourceId": "azureml-rockwellautomation", "entityContainerType": "Registry"},
                {"resourceId": "azureml-bayer", "entityContainerType": "Registry"},
                {"resourceId": "azureml-cerence", "entityContainerType": "Registry"},
                {"resourceId": "azureml-sight-machine", "entityContainerType": "Registry"},
                {"resourceId": "azureml-deepseek", "entityContainerType": "Registry"},
                {"resourceId": "azureml-stabilityai", "entityContainerType": "Registry"},
                {"resourceId": "azureml-xai", "entityContainerType": "Registry"},
                {"resourceId": "azureml-blackforestlabs", "entityContainerType": "Registry"}
            ],
            "indexEntitiesRequest": {
                "filters": [
                    {"field": "type", "operator": "eq", "values": ["models"]},
                    {"field": "kind", "operator": "eq", "values": ["Versioned"]},
                    {"field": "properties/isAnonymous", "operator": "ne", "values": ["true"]},
                    {"field": "annotations/archived", "operator": "ne", "values": ["true"]},
                    {"field": "properties/userProperties/is-promptflow", "operator": "notexists"},
                    {"field": "labels", "operator": "eq", "values": ["latest"]},
                    {"field": "properties/name", "operator": "eq", "values": ["dall-e-3", "gpt-35-turbo", "gpt-35-turbo-16k", "gpt-4", "gpt-4-32k", "gpt-4o", "gpt-4o-mini", "gpt-4o-audio-preview", "gpt-4.1", "gpt-4.1-mini", "gpt-4.1-nano", "o1", "o3-mini", "o4-mini", "ada", "text-embedding-ada-002", "babbage", "curie", "davinci", "text-embedding-3-small", "text-embedding-3-large", "AI21-Jamba-1.5-Large", "AI21-Jamba-1.5-Mini", "AI21-Jamba-Instruct", "Codestral-2501", "cohere-command-a", "Cohere-command-r", "Cohere-command-r-08-2024", "Cohere-command-r-plus", "Cohere-command-r-plus-08-2024", "Cohere-embed-v3-english", "Cohere-embed-v3-multilingual", "DeepSeek-R1", "DeepSeek-R1-0528", "DeepSeek-V3", "DeepSeek-V3-0324", "embed-v-4-0", "jais-30b-chat", "Llama-3.2-11B-Vision-Instruct", "Llama-3.2-90B-Vision-Instruct", "Llama-3.3-70B-Instruct", "MAI-DS-R1", "Meta-Llama-3-70B-Instruct", "Meta-Llama-3-8B-Instruct", "Meta-Llama-3.1-405B-Instruct", "Meta-Llama-3.1-70B-Instruct", "Meta-Llama-3.1-8B-Instruct", "Ministral-3B", "Mistral-large-2407", "Mistral-Large-2411", "Mistral-Nemo", "Mistral-small", "mistral-small-2503", "Phi-3-medium-128k-instruct", "Phi-3-medium-4k-instruct", "Phi-3-mini-128k-instruct", "Phi-3-mini-4k-instruct", "Phi-3-small-128k-instruct", "Phi-3-small-8k-instruct", "Phi-3.5-mini-instruct", "Phi-3.5-MoE-instruct", "Phi-3.5-vision-instruct", "Phi-4", "Phi-4-mini-instruct", "Phi-4-multimodal-instruct", "Phi-4-reasoning", "Phi-4-mini-reasoning", "Llama-4-Maverick-17B-128E-Instruct-FP8", "Llama-4-Scout-17B-16E-Instruct", "mistral-medium-2505", "mistral-document-ai-2505", "grok-3", "grok-3-mini", "FLUX-1.1-pro", "FLUX.1-Kontext-pro", "gpt-oss-120b", "code-cushman-001", "code-cushman-fine-tune-002", "whisper", "text-ada-001", "text-similarity-ada-001", "text-search-ada-doc-001", "text-search-ada-query-001", "code-search-ada-code-001", "code-search-ada-text-001", "text-babbage-001", "text-similarity-babbage-001", "text-search-babbage-doc-001", "text-search-babbage-query-001", "code-search-babbage-code-001", "code-search-babbage-text-001", "text-curie-001", "text-similarity-curie-001", "text-search-curie-doc-001", "text-search-curie-query-001", "text-davinci-001", "text-davinci-002", "text-davinci-003", "text-davinci-fine-tune-002", "code-davinci-002", "code-davinci-fine-tune-002", "text-similarity-davinci-001", "text-search-davinci-doc-001", "text-search-davinci-query-001", "dall-e-2", "gpt-35-turbo-instruct", "gpt-4o-mini-audio-preview", "o1-mini", "gpt-4o-mini-realtime-preview", "gpt-4o-realtime-preview", "gpt-4o-transcribe", "gpt-4o-mini-transcribe", "gpt-4o-mini-tts", "gpt-5-mini", "gpt-5-nano", "gpt-5-chat", "model-router", "codex-mini", "sora", "tts", "tts-hd", "babbage-002", "davinci-002", "Azure-AI-Speech", "Azure-AI-Language", "Azure-AI-Vision", "Azure-AI-Translator", "Azure-AI-Content-Understanding", "Azure-AI-Document-Intelligence", "Azure-AI-Content-Safety"]}
                ],
                "freeTextSearch": "",
                "order": [{"field": "properties/name", "direction": "Asc"}],
                "pageSize": 30,
                "facets": [ ],
                "includeTotalResultCount": true,
                "searchBuilder": "AppendPrefix"
            }
        }
        """;

        string jsonContent;
        if (continuationToken != null)
        {
            var root = JsonNode.Parse(basePayload)?.AsObject()!;
            var indexEntitiesRequest = root["indexEntitiesRequest"]?.AsObject()!;
            indexEntitiesRequest.Add("continuationToken", continuationToken);
            indexEntitiesRequest["includeTotalResultCount"] = false;

            jsonContent = root.ToJsonString();
        }
        else
        {
            jsonContent = basePayload;
        }

        request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request).ConfigureAwait(false);

        // Handle decompression automatically
        var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        return content;
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
        _handler?.Dispose();
    }
}

// Response Models
public class ApiResponse
{
    [JsonPropertyName("indexEntitiesResponse")]
    public IndexEntitiesResponse? IndexEntitiesResponse { get; set; }

    [JsonPropertyName("regionalErrors")]
    public object? RegionalErrors { get; set; }

    [JsonPropertyName("resourceSkipReasons")]
    public object? ResourceSkipReasons { get; set; }

    [JsonPropertyName("shardErrors")]
    public object? ShardErrors { get; set; }

    [JsonPropertyName("numberOfResourcesNotIncludedInSearch")]
    public int NumberOfResourcesNotIncludedInSearch { get; set; }
}

public class IndexEntitiesResponse
{
    [JsonPropertyName("totalCount")]
    public int? TotalCount { get; set; }

    [JsonPropertyName("value")]
    public List<ModelEntity>? Value { get; set; }

    [JsonPropertyName("continuationToken")]
    public string? ContinuationToken { get; set; }
}

public class ModelEntity
{
    [JsonPropertyName("relevancyScore")]
    public double RelevancyScore { get; set; }

    [JsonPropertyName("entityResourceName")]
    public string? EntityResourceName { get; set; }

    [JsonPropertyName("highlights")]
    public Dictionary<string, object>? Highlights { get; set; }

    [JsonPropertyName("schemaId")]
    public string? SchemaId { get; set; }

    [JsonPropertyName("entityId")]
    public string? EntityId { get; set; }

    [JsonPropertyName("kind")]
    public string? Kind { get; set; }

    [JsonPropertyName("annotations")]
    public ModelAnnotations? Annotations { get; set; }

    [JsonPropertyName("properties")]
    public ModelProperties? Properties { get; set; }

    [JsonPropertyName("internal")]
    public Dictionary<string, object>? Internal { get; set; }

    [JsonPropertyName("updateSequence")]
    public int UpdateSequence { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("version")]
    public string? Version { get; set; }

    [JsonPropertyName("entityContainerId")]
    public string? EntityContainerId { get; set; }

    [JsonPropertyName("entityObjectId")]
    public string? EntityObjectId { get; set; }

    [JsonPropertyName("resourceType")]
    public string? ResourceType { get; set; }

    [JsonPropertyName("relationships")]
    public List<object>? Relationships { get; set; }

    [JsonPropertyName("assetId")]
    public string? AssetId { get; set; }

    [JsonPropertyName("usage")]
    public ModelUsage? Usage { get; set; }

    [JsonPropertyName("isAFragment")]
    public bool IsAFragment { get; set; }

    [JsonPropertyName("fragmentId")]
    public string? FragmentId { get; set; }
}

public class ModelAnnotations
{
    [JsonPropertyName("invisibleUntil")]
    public string? InvisibleUntil { get; set; }

    [JsonPropertyName("archived")]
    public bool Archived { get; set; }

    [JsonPropertyName("tags")]
    public Dictionary<string, string>? Tags { get; set; }

    [JsonPropertyName("datasets")]
    public List<object>? Datasets { get; set; }

    [JsonPropertyName("sampleInputData")]
    public object? SampleInputData { get; set; }

    [JsonPropertyName("sampleOutputData")]
    public object? SampleOutputData { get; set; }

    [JsonPropertyName("resourceRequirements")]
    public object? ResourceRequirements { get; set; }

    [JsonPropertyName("stage")]
    public string? Stage { get; set; }

    [JsonPropertyName("systemCatalogData")]
    public SystemCatalogData? SystemCatalogData { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("labels")]
    public List<string>? Labels { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

public class SystemCatalogData
{
    [JsonPropertyName("publisher")]
    public string? Publisher { get; set; }

    [JsonPropertyName("modelCapabilities")]
    public List<string>? ModelCapabilities { get; set; }

    [JsonPropertyName("deploymentTypes")]
    public List<string>? DeploymentTypes { get; set; }

    [JsonPropertyName("license")]
    public string? License { get; set; }

    [JsonPropertyName("inferenceTasks")]
    public List<string>? InferenceTasks { get; set; }

    [JsonPropertyName("fineTuningTasks")]
    public List<string>? FineTuningTasks { get; set; }

    [JsonPropertyName("inferenceComputeAllowed")]
    public object? InferenceComputeAllowed { get; set; }

    [JsonPropertyName("fineTuneComputeAllowed")]
    public object? FineTuneComputeAllowed { get; set; }

    [JsonPropertyName("evaluationComputeAllowed")]
    public object? EvaluationComputeAllowed { get; set; }

    [JsonPropertyName("languages")]
    public List<string>? Languages { get; set; }

    [JsonPropertyName("summary")]
    public string? Summary { get; set; }

    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }

    [JsonPropertyName("textContextWindow")]
    public int? TextContextWindow { get; set; }

    [JsonPropertyName("maxOutputTokens")]
    public int? MaxOutputTokens { get; set; }

    [JsonPropertyName("inputModalities")]
    public List<string>? InputModalities { get; set; }

    [JsonPropertyName("outputModalities")]
    public List<string>? OutputModalities { get; set; }

    [JsonPropertyName("playgroundRateLimitTier")]
    public string? PlaygroundRateLimitTier { get; set; }

    [JsonPropertyName("azureOffers")]
    public List<string>? AzureOffers { get; set; }
}

public class ModelProperties
{
    [JsonPropertyName("updatedTime")]
    public string? UpdatedTime { get; set; }

    [JsonPropertyName("creationContext")]
    public CreationContext? CreationContext { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("modelFramework")]
    public string? ModelFramework { get; set; }

    [JsonPropertyName("modelFrameworkVersion")]
    public string? ModelFrameworkVersion { get; set; }

    [JsonPropertyName("modelFormat")]
    public string? ModelFormat { get; set; }

    [JsonPropertyName("version")]
    public int Version { get; set; }

    [JsonPropertyName("alphanumericVersion")]
    public string? AlphanumericVersion { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("mimeType")]
    public string? MimeType { get; set; }

    [JsonPropertyName("modifiedTime")]
    public string? ModifiedTime { get; set; }

    [JsonPropertyName("unpack")]
    public bool Unpack { get; set; }

    [JsonPropertyName("parentModelId")]
    public string? ParentModelId { get; set; }

    [JsonPropertyName("runId")]
    public string? RunId { get; set; }

    [JsonPropertyName("experimentName")]
    public string? ExperimentName { get; set; }

    [JsonPropertyName("derivedModelIds")]
    public object? DerivedModelIds { get; set; }

    [JsonPropertyName("userProperties")]
    public Dictionary<string, string>? UserProperties { get; set; }

    [JsonPropertyName("isAnonymous")]
    public bool IsAnonymous { get; set; }

    [JsonPropertyName("orginAssetId")]
    public string? OrginAssetId { get; set; }

    [JsonPropertyName("intellectualProperty")]
    public IntellectualProperty? IntellectualProperty { get; set; }

    [JsonPropertyName("variantInfo")]
    public object? VariantInfo { get; set; }

    [JsonPropertyName("provisioningState")]
    public string? ProvisioningState { get; set; }
}

public class CreationContext
{
    [JsonPropertyName("createdTime")]
    public string? CreatedTime { get; set; }

    [JsonPropertyName("createdBy")]
    public CreatedBy? CreatedBy { get; set; }

    [JsonPropertyName("creationSource")]
    public string? CreationSource { get; set; }
}

public class CreatedBy
{
    [JsonPropertyName("userObjectId")]
    public string? UserObjectId { get; set; }

    [JsonPropertyName("userTenantId")]
    public string? UserTenantId { get; set; }

    [JsonPropertyName("userName")]
    public string? UserName { get; set; }

    [JsonPropertyName("userPrincipalName")]
    public string? UserPrincipalName { get; set; }
}

public class IntellectualProperty
{
    [JsonPropertyName("publisher")]
    public string? Publisher { get; set; }
}

public class ModelUsage
{
    [JsonPropertyName("totalCount")]
    public int TotalCount { get; set; }

    [JsonPropertyName("popularity")]
    public double Popularity { get; set; }
}

public class ConsolidatedResponse
{
    [JsonPropertyName("totalCount")]
    public int TotalCount { get; set; }

    [JsonPropertyName("pagesCombined")]
    public string? PagesCombined { get; set; }

    [JsonPropertyName("entities")]
    public List<ModelEntity>? Entities { get; set; }
}

public class ModelClassGenerator
{
    public string GenerateCode(string csNamespace, List<ModelEntity> models)
    {
        var sb = new StringBuilder();
        // Add file header
        sb.AppendLine("// Licensed to the .NET Foundation under one or more agreements.");
        sb.AppendLine("// The .NET Foundation licenses this file to you under the MIT license.");
        sb.AppendLine(CultureInfo.InvariantCulture, $"namespace {csNamespace};");
        sb.AppendLine();
        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// Generated strongly typed model descriptors for Azure AI Foundry.");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("public partial class AIFoundryModel");
        sb.AppendLine("{");

        // Group models by publisher (only include models that are visible and have names & publishers)
        var modelsByPublisher = models
            .Where(m => m.Annotations?.SystemCatalogData?.Publisher != null &&
                        m.Annotations?.Name != null &&
                        IsVisible(m.Annotations?.InvisibleUntil))
            .GroupBy(m => m.Annotations!.SystemCatalogData!.Publisher!)
            .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase);

        foreach (var publisherGroup in modelsByPublisher)
        {
            var publisherClassName = GeneratePublisherClassName(publisherGroup.Key);
            sb.AppendLine(CultureInfo.InvariantCulture, $"    /// <summary>");
            sb.AppendLine(CultureInfo.InvariantCulture, $"    /// Models published by {EscapeXml(publisherGroup.Key)}.");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine(CultureInfo.InvariantCulture, $"    public static class {publisherClassName}");
            sb.AppendLine("    {");

            foreach (var model in publisherGroup.OrderBy(m => m.Annotations!.Name, StringComparer.OrdinalIgnoreCase))
            {
                var modelName = model.Annotations!.Name!;
                var descriptorName = GenerateMethodName(modelName); // Reuse method name logic for descriptor property name
                var version = GetModelVersion(model);
                var publisher = model.Annotations!.SystemCatalogData!.Publisher!;
                var description = CleanDescription(model.Annotations?.SystemCatalogData?.Summary ?? model.Annotations?.Description ?? $"Descriptor for {modelName} model");

                sb.AppendLine("        /// <summary>");
                sb.AppendLine(CultureInfo.InvariantCulture, $"        /// {EscapeXml(description)}");
                sb.AppendLine("        /// </summary>");
                sb.AppendLine(CultureInfo.InvariantCulture, $"        public static readonly AIFoundryModel {descriptorName} = new() {{ Name = \"{EscapeStringForCSharp(modelName)}\", Version = \"{EscapeStringForCSharp(version)}\", Format = \"{EscapeStringForCSharp(publisher)}\" }};");
                sb.AppendLine();
            }

            sb.AppendLine("    }");
            sb.AppendLine();
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

    private static string EscapeXml(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }
        return value.Replace("&", "&amp;")
                    .Replace("<", "&lt;")
                    .Replace(">", "&gt;")
                    .Replace("\"", "&quot;")
                    .Replace("'", "&apos;");
    }

    private static string GenerateMethodName(string modelName)
    {
        // Convert model name to PascalCase method name
        var parts = modelName
            .Replace("-", " ")
            .Replace(".", " ")
            .Replace("_", " ")
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);

        var result = new StringBuilder();
        foreach (var part in parts)
        {
            if (part.Length > 0)
            {
                // Handle special cases for numbers and versions
                if (char.IsDigit(part[0]))
                {
                    result.Append(part);
                }
                else
                {
                    result.Append(char.ToUpper(part[0]));
                    if (part.Length > 1)
                    {
                        result.Append(part.Substring(1).ToLower());
                    }
                }
            }
        }

        return result.ToString();
    }

    private static string GetModelVersion(ModelEntity model)
    {
        // Try to get version from different sources
        if (!string.IsNullOrEmpty(model.Properties?.AlphanumericVersion))
        {
            return model.Properties.AlphanumericVersion;
        }

        if (!string.IsNullOrEmpty(model.Version))
        {
            return model.Version;
        }

        if (model.Properties?.Version > 0)
        {
            return model.Properties.Version.ToString(CultureInfo.InvariantCulture);
        }

        return "1"; // Default version
    }

    private static string CleanDescription(string description)
    {
        if (string.IsNullOrEmpty(description))
        {
            return "AI model deployment";
        }

        // Clean up description for XML documentation
        return description
            .Replace("\n", " ")
            .Replace("\r", " ")
            .Replace("  ", " ")
            .Trim();
    }

    private static string EscapeStringForCSharp(string value)
    {
        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"");
    }

    private static string GeneratePublisherClassName(string publisher)
    {
        // Similar logic to GenerateMethodName but keep acronyms in PascalCase style
        var cleaned = publisher
            .Replace("-", " ")
            .Replace(".", " ")
            .Replace("_", " ");

        var parts = cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var sb = new StringBuilder();
        foreach (var part in parts)
        {
            if (part.Length == 0)
            {
                continue;
            }
            if (part.All(char.IsUpper) && part.Length > 1)
            {
                // e.g. AI -> Ai
                sb.Append(char.ToUpperInvariant(part[0]));
                // Use span overloads where possible
                // Use substring for lowercasing efficiently
                sb.Append(part.Substring(1).ToLowerInvariant());
            }
            else
            {
                sb.Append(char.ToUpperInvariant(part[0]));
                if (part.Length > 1)
                {
                    sb.Append(part.AsSpan(1));
                }
            }
        }
        return sb.ToString();
    }

    private static bool IsVisible(string? invisibleUntil)
    {
        if (string.IsNullOrWhiteSpace(invisibleUntil))
        {
            return true; // No restriction
        }
        if (DateTime.TryParse(invisibleUntil, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dt))
        {
            return dt <= DateTime.UtcNow;
        }
        return true; // If parse fails, be permissive
    }
}
