// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Azure.AI.OpenAI;
using Aspire;

[assembly: ConfigurationSchema("Aspire:Azure:AI:OpenAI", typeof(AzureOpenAISettings))]

[assembly: LoggingCategories(
    "Azure.AI.OpenAI"
)]
