// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.OpenAI;
using Aspire;
using Microsoft.Extensions.Hosting;

[assembly: ConfigurationSchema("Aspire:OpenAI", typeof(OpenAISettings))]
[assembly: ConfigurationSchema("Aspire:OpenAI:ClientOptions", typeof(AspireOpenAIExtensions))]

[assembly: LoggingCategories(
    "Azure",
    "Azure.Core",
    "Azure.Identity"
)]
