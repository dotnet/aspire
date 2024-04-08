// Assembly 'Aspire.Azure.AI.OpenAI'

using System;
using Aspire.Azure.AI.OpenAI;
using Aspire.Azure.Common;
using Azure.AI.OpenAI;
using Azure.Core;
using Azure.Core.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Provides extension methods for registering <see cref="T:Azure.AI.OpenAI.OpenAIClient" /> as a singleton in the services provided by the <see cref="T:Microsoft.Extensions.Hosting.IHostApplicationBuilder" />.
/// </summary>
public static class AspireAzureOpenAIExtensions
{
    /// <summary>
    /// Registers <see cref="T:Azure.AI.OpenAI.OpenAIClient" /> as a singleton in the services provided by the <paramref name="builder" />.
    /// </summary>
    /// <param name="builder">The <see cref="T:Microsoft.Extensions.Hosting.IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="connectionName">A name used to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="T:Aspire.Azure.AI.OpenAI.AzureOpenAISettings" />. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureClientBuilder">An optional method that can be used for customizing the <see cref="T:Azure.Core.Extensions.IAzureClientBuilder`2" />.</param>
    /// <remarks>Reads the configuration from "Aspire.Azure.AI.OpenAI" section.</remarks>
    public static void AddAzureOpenAIClient(this IHostApplicationBuilder builder, string connectionName, Action<AzureOpenAISettings>? configureSettings = null, Action<IAzureClientBuilder<OpenAIClient, OpenAIClientOptions>>? configureClientBuilder = null);

    /// <summary>
    /// Registers <see cref="T:Azure.AI.OpenAI.OpenAIClient" /> as a singleton for given <paramref name="name" /> in the services provided by the <paramref name="builder" />.
    /// </summary>
    /// <param name="builder">The <see cref="T:Microsoft.Extensions.Hosting.IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="name">The name of the component, which is used as the <see cref="P:Microsoft.Extensions.DependencyInjection.ServiceDescriptor.ServiceKey" /> of the service and also to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="T:Aspire.Azure.AI.OpenAI.AzureOpenAISettings" />. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureClientBuilder">An optional method that can be used for customizing the <see cref="T:Azure.Core.Extensions.IAzureClientBuilder`2" />.</param>
    /// <remarks>Reads the configuration from "Aspire.Azure.AI.OpenAI:{name}" section.</remarks>
    public static void AddKeyedAzureOpenAIClient(this IHostApplicationBuilder builder, string name, Action<AzureOpenAISettings>? configureSettings = null, Action<IAzureClientBuilder<OpenAIClient, OpenAIClientOptions>>? configureClientBuilder = null);
}
