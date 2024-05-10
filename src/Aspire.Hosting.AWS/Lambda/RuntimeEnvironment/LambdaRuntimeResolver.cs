// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.AWS.Lambda.RuntimeEnvironment;

internal static class LambdaRuntimeResolver
{
    private static bool IsMockToolDisabled(this IResourceBuilder<ILambdaFunction> builder)
    {
        // Globally disabled.
        if (string.Equals(Environment.GetEnvironmentVariable(Constants.MockToolsLambdaDisable), "true", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Relevant when adding multiple functions from same class library.
        return builder.ApplicationBuilder.Resources.OfType<ILambdaFunction>()
            .Where(x => x.GetFunctionMetadata().ProjectPath == builder.Resource.GetFunctionMetadata().ProjectPath)
            .Any(x => x.Annotations.OfType<MockToolLambdaDisabledAnnotation>().Any());
    }

    public static IResourceBuilder<ILambdaFunction> ResolveLambdaRuntime(this IResourceBuilder<ILambdaFunction> builder, Action<MockToolLambdaConfiguration>? configureMockTool)
    {
        var functionMetadata = builder.Resource.GetFunctionMetadata();

        // https://github.com/aws/aws-lambda-dotnet/tree/master/Libraries/src/Amazon.Lambda.AspNetCoreServer.Hosting
        // Lambda Function can run as a regular project and does not need the Mock Tool.
        if (functionMetadata.Traits.Contains("Amazon.Lambda.AspNetCoreServer.Hosting"))
        {
            var projectBuilder = builder.ApplicationBuilder.AddProject($"{builder.Resource.Name}-Project", functionMetadata.ProjectPath)
                .ExcludeFromManifest();

            builder.WithAnnotation(new LambdaRuntimeEnvironmentAnnotation(projectBuilder.Resource.GetEndpoints().ToArray()));

            return builder;
        }

        if (builder.IsMockToolDisabled())
        {
            return builder;
        }

        var configuration = new MockToolLambdaConfiguration();
        configureMockTool?.Invoke(configuration);

        if (configuration.Disabled)
        {
            builder.WithAnnotation(new MockToolLambdaDisabledAnnotation());
            return builder;
        }

        var mockToolExecutable = builder.ApplicationBuilder.Resources.OfType<MockToolLambdaRuntimeEnvironment>()
            .SingleOrDefault(x => x.ProjectPath == functionMetadata.ProjectPath);

        if (mockToolExecutable != null)
        {
            builder.WithAnnotation(new LambdaRuntimeEnvironmentAnnotation(mockToolExecutable.GetEndpoint("http")));
            return builder;
        }

        // TODO: Check if Tool has been installed before proceeding

        var executables = builder.ApplicationBuilder.Resources.OfType<MockToolLambdaRuntimeEnvironment>().ToArray();

        const int defaultMockToolPort = 5050;
        var port = configuration.Port;

        // When default port is configured, ensure multiple instances receive their own port
        while (configuration.Port == defaultMockToolPort)
        {
            if (executables.Any(x => x.Port == port))
            {
                port++;
                continue;
            }

            configuration.Port = port;
            break;
        }

        var name = builder.Resource.IsExecutableProject()
            ? builder.Resource.Name
            : functionMetadata.Handler.Split("::").First().Replace(".", "-").Replace("_", "-");

        mockToolExecutable = new MockToolLambdaRuntimeEnvironment($"{name}-MockTool", functionMetadata,
            configuration, builder.Resource.Runtime.Name);

        builder.ApplicationBuilder.AddResource(mockToolExecutable)
            .ExcludeFromManifest()
            .WithHttpEndpoint(port, isProxied: false, name: "http");

        // Lambda Class Library, do not create a project.
        if (!builder.Resource.IsExecutableProject())
        {
            builder.WithAnnotation(new LambdaRuntimeEnvironmentAnnotation(mockToolExecutable.GetEndpoint("http")));
            return builder;
        }

        builder.WithAnnotation(new LambdaRuntimeEnvironmentAnnotation(mockToolExecutable.GetEndpoint("http"))
        {
            PathAndQuery = "runtime"
        });

        builder.ApplicationBuilder.AddProject($"{builder.Resource.Name}-Project", functionMetadata.ProjectPath)
            .ExcludeFromManifest()
            .WithEnvironment(ctx =>
            {
                if (ctx.ExecutionContext.IsPublishMode)
                {
                    return;
                }
                var ep = mockToolExecutable.GetEndpoint("http");
                ctx.EnvironmentVariables["AWS_LAMBDA_RUNTIME_API"] = $"{ep.Host}:{ep.Port.ToString(CultureInfo.InvariantCulture)}";
            })
            .WithEndpoint("mockTool", annotation =>
            {
                annotation.Port = port;
                annotation.TargetPort = port;
                annotation.IsProxied = false;
                annotation.UriScheme = "http";
            });

        return builder;
    }
}
