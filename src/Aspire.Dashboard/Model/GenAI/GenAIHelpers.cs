// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Otlp.Model;

namespace Aspire.Dashboard.Model.GenAI;

public static class GenAIHelpers
{
    public const string GenAISystem = "gen_ai.system";
    public const string GenAIProviderName = "gen_ai.provider.name";
    public const string GenAIEventContent = "gen_ai.event.content";
    public const string GenAISystemInstructions = "gen_ai.system_instructions";
    public const string GenAIInputMessages = "gen_ai.input.messages";
    public const string GenAIOutputInstructions = "gen_ai.output.messages";
    public const string GenAIResponseModel = "gen_ai.response.model";
    public const string GenAIUsageInputTokens = "gen_ai.usage.input_tokens";
    public const string GenAIUsageOutputTokens = "gen_ai.usage.output_tokens";

    public static bool IsGenAISpan(KeyValuePair<string, string>[] attributes)
    {
        return attributes.GetValueWithFallback(GenAISystem, GenAIProviderName) is { Length: > 0 };
    }
}
