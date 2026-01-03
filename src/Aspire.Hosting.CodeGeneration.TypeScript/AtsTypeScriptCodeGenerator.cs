// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Aspire.Hosting.CodeGeneration.Models;

namespace Aspire.Hosting.CodeGeneration.TypeScript;

/// <summary>
/// Generates a simplified TypeScript SDK using the ATS (Aspire Type System) capability-based API.
/// This generator produces a hand-crafted SDK that uses invokeCapability instead of reflection-based RPC.
/// </summary>
public sealed class AtsTypeScriptCodeGenerator : ICodeGenerator
{
    /// <inheritdoc />
    public string Language => "TypeScript";

    /// <inheritdoc />
    public Dictionary<string, string> GenerateDistributedApplication(ApplicationModel model)
    {
        var files = new Dictionary<string, string>();

        // Add embedded resource files (types.ts, RemoteAppHostClient.ts)
        files["types.ts"] = GetEmbeddedResource("types.ts");
        files["RemoteAppHostClient.ts"] = GetEmbeddedResource("RemoteAppHostClient.ts");

        // Generate the capability-based aspire.ts SDK
        files["aspire.ts"] = GenerateAspireSdk();

        return files;
    }

    private static string GetEmbeddedResource(string name)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"Aspire.Hosting.CodeGeneration.TypeScript.Resources.{name}";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{name}' not found.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    /// <summary>
    /// Generates the aspire.ts SDK file with capability-based API.
    /// </summary>
    private static string GenerateAspireSdk()
    {
        return """
            // aspire.ts - Capability-based Aspire SDK
            // This SDK uses the ATS (Aspire Type System) capability API.
            // Capabilities are versioned endpoints like 'aspire/createBuilder@1'.

            import {
                RemoteAppHostClient,
                Handle,
                CapabilityError,
                registerCallback,
                wrapIfProxy
            } from './RemoteAppHostClient.js';

            // ============================================================================
            // Handle Type Aliases
            // ============================================================================

            /** Handle to IDistributedApplicationBuilder */
            export type BuilderHandle = Handle<'aspire/Builder'>;

            /** Handle to DistributedApplication */
            export type ApplicationHandle = Handle<'aspire/Application'>;

            /** Handle to DistributedApplicationExecutionContext */
            export type ExecutionContextHandle = Handle<'aspire/ExecutionContext'>;

            /** Handle to IResourceBuilder<ContainerResource> */
            export type ContainerBuilderHandle = Handle<'aspire/ContainerBuilder'>;

            /** Handle to IResourceBuilder<T> where T : IResourceWithEnvironment */
            export type ResourceWithEnvironmentHandle = Handle<'aspire/IResourceWithEnvironment'>;

            /** Handle to EndpointReference */
            export type EndpointReferenceHandle = Handle<'aspire/EndpointReference'>;

            /** Handle to EnvironmentCallbackContext */
            export type EnvironmentContextHandle = Handle<'aspire/EnvironmentContext'>;

            // ============================================================================
            // Aspire Client - Wraps RemoteAppHostClient with typed capability methods
            // ============================================================================

            /**
             * High-level Aspire client that provides typed access to ATS capabilities.
             */
            export class AspireClient {
                constructor(private readonly rpc: RemoteAppHostClient) {}

                /** Get the underlying RPC client */
                get client(): RemoteAppHostClient {
                    return this.rpc;
                }

                /**
                 * Invokes a capability by ID with the given arguments.
                 * Use this for capabilities not exposed as typed methods.
                 */
                async invokeCapability<T>(
                    capabilityId: string,
                    args?: Record<string, unknown>
                ): Promise<T> {
                    return await this.rpc.invokeCapability<T>(capabilityId, args ?? {});
                }

                // ========================================================================
                // Application Lifecycle
                // ========================================================================

                /**
                 * Creates a new distributed application builder.
                 * This is the entry point for building Aspire applications.
                 */
                async createBuilder(args?: string[]): Promise<BuilderHandle> {
                    return await this.rpc.invokeCapability<BuilderHandle>(
                        'aspire/createBuilder@1',
                        { args }
                    );
                }

                /**
                 * Builds the distributed application from the configured builder.
                 */
                async build(builder: BuilderHandle): Promise<ApplicationHandle> {
                    return await this.rpc.invokeCapability<ApplicationHandle>(
                        'aspire/build@1',
                        { builder }
                    );
                }

                /**
                 * Runs the distributed application, starting all configured resources.
                 */
                async run(app: ApplicationHandle): Promise<void> {
                    await this.rpc.invokeCapability<void>(
                        'aspire/run@1',
                        { app }
                    );
                }

                // ========================================================================
                // Execution Context
                // ========================================================================

                /**
                 * Gets the execution context from the builder.
                 */
                async getExecutionContext(builder: BuilderHandle): Promise<ExecutionContextHandle> {
                    return await this.rpc.invokeCapability<ExecutionContextHandle>(
                        'aspire/getExecutionContext@1',
                        { builder }
                    );
                }

                /**
                 * Checks if the application is running in run mode.
                 */
                async isRunMode(context: ExecutionContextHandle): Promise<boolean> {
                    return await this.rpc.invokeCapability<boolean>(
                        'aspire/isRunMode@1',
                        { context }
                    );
                }

                /**
                 * Checks if the application is running in publish mode.
                 */
                async isPublishMode(context: ExecutionContextHandle): Promise<boolean> {
                    return await this.rpc.invokeCapability<boolean>(
                        'aspire/isPublishMode@1',
                        { context }
                    );
                }

                // ========================================================================
                // Container Resources
                // ========================================================================

                /**
                 * Adds a container resource to the application.
                 */
                async addContainer(
                    builder: BuilderHandle,
                    name: string,
                    image: string
                ): Promise<ContainerBuilderHandle> {
                    return await this.rpc.invokeCapability<ContainerBuilderHandle>(
                        'aspire/addContainer@1',
                        { builder, name, image }
                    );
                }

                // ========================================================================
                // Resource Configuration (works on any resource with environment)
                // ========================================================================

                /**
                 * Sets an environment variable on a resource.
                 * Returns the same handle for chaining.
                 */
                async withEnvironment<T extends Handle>(
                    builder: T,
                    name: string,
                    value: string
                ): Promise<T> {
                    return await this.rpc.invokeCapability<T>(
                        'aspire/withEnvironment@1',
                        { builder, name, value }
                    );
                }

                /**
                 * Adds an environment callback to a resource.
                 * The callback is invoked during resource startup.
                 * Note: This uses 'resource' param name (from CoreExports.cs), not 'builder'
                 */
                async withEnvironmentCallback<T extends Handle>(
                    resource: T,
                    callback: (context: EnvironmentContextHandle) => Promise<void>
                ): Promise<T> {
                    const callbackId = registerCallback(async (contextData: unknown) => {
                        const context = wrapIfProxy(contextData) as EnvironmentContextHandle;
                        await callback(context);
                    });

                    return await this.rpc.invokeCapability<T>(
                        'aspire/withEnvironmentCallback@1',
                        { resource, callback: callbackId }
                    );
                }

                // ========================================================================
                // Endpoints
                // ========================================================================

                /**
                 * Gets an endpoint reference from a resource.
                 */
                async getEndpoint(
                    builder: Handle,
                    name: string
                ): Promise<EndpointReferenceHandle> {
                    return await this.rpc.invokeCapability<EndpointReferenceHandle>(
                        'aspire/getEndpoint@1',
                        { builder, name }
                    );
                }

                /**
                 * Adds an HTTP endpoint to a resource.
                 */
                async withHttpEndpoint<T extends Handle>(
                    builder: T,
                    options?: {
                        port?: number;
                        targetPort?: number;
                        name?: string;
                        env?: string;
                        isProxied?: boolean;
                    }
                ): Promise<T> {
                    return await this.rpc.invokeCapability<T>(
                        'aspire/withHttpEndpoint@1',
                        { builder, ...options }
                    );
                }

                // ========================================================================
                // Volumes
                // ========================================================================

                /**
                 * Adds a volume to a container resource.
                 */
                async withVolume(
                    builder: ContainerBuilderHandle,
                    target: string,
                    name?: string,
                    isReadOnly?: boolean
                ): Promise<ContainerBuilderHandle> {
                    return await this.rpc.invokeCapability<ContainerBuilderHandle>(
                        'aspire/withVolume@1',
                        { builder, target, name, isReadOnly }
                    );
                }

                // ========================================================================
                // Dependencies
                // ========================================================================

                /**
                 * Adds a reference from one resource to another.
                 * Note: Uses 'resource' param name (from CoreExports.cs)
                 */
                async withReference<T extends Handle>(
                    resource: T,
                    dependency: Handle
                ): Promise<T> {
                    return await this.rpc.invokeCapability<T>(
                        'aspire/withReference@1',
                        { resource, dependency }
                    );
                }

                /**
                 * Waits for another resource to be ready before starting.
                 * Note: Uses 'builder' param name (from ResourceBuilderExtensions.cs)
                 */
                async waitFor<T extends Handle>(
                    builder: T,
                    dependency: Handle
                ): Promise<T> {
                    return await this.rpc.invokeCapability<T>(
                        'aspire/waitFor@1',
                        { builder, dependency }
                    );
                }

                // ========================================================================
                // Utility
                // ========================================================================

                /**
                 * Gets the name of a resource.
                 * Note: Uses 'resource' param name (from CoreExports.cs)
                 */
                async getResourceName(resource: Handle): Promise<string> {
                    return await this.rpc.invokeCapability<string>(
                        'aspire/getResourceName@1',
                        { resource }
                    );
                }

                /**
                 * Lists all available capabilities from the server.
                 */
                async getCapabilities(): Promise<string[]> {
                    return await this.rpc.getCapabilities();
                }
            }

            // ============================================================================
            // Connection Helper
            // ============================================================================

            /**
             * Creates and connects an AspireClient.
             * Reads connection info from environment variables set by `aspire run`.
             */
            export async function connect(): Promise<AspireClient> {
                const socketPath = process.env.REMOTE_APP_HOST_SOCKET_PATH;
                if (!socketPath) {
                    throw new Error(
                        'REMOTE_APP_HOST_SOCKET_PATH environment variable not set. ' +
                        'Run this application using `aspire run`.'
                    );
                }

                const authToken = process.env.ASPIRE_RPC_AUTH_TOKEN;
                if (!authToken) {
                    throw new Error(
                        'ASPIRE_RPC_AUTH_TOKEN environment variable not set. ' +
                        'Run this application using `aspire run`.'
                    );
                }

                const rpc = new RemoteAppHostClient(socketPath);
                await rpc.connect();
                await rpc.authenticate(authToken);

                return new AspireClient(rpc);
            }

            // Re-export commonly used types
            export { Handle, CapabilityError, registerCallback } from './RemoteAppHostClient.js';

            // ============================================================================
            // Global Error Handling
            // ============================================================================

            /**
             * Set up global error handlers to ensure the process exits properly on errors.
             * Node.js doesn't exit on unhandled rejections by default, so we need to handle them.
             */
            process.on('unhandledRejection', (reason: unknown) => {
                const error = reason instanceof Error ? reason : new Error(String(reason));

                if (reason instanceof CapabilityError) {
                    console.error(`\n❌ Capability Error: ${error.message}`);
                    console.error(`   Code: ${(reason as CapabilityError).code}`);
                    if ((reason as CapabilityError).capability) {
                        console.error(`   Capability: ${(reason as CapabilityError).capability}`);
                    }
                } else {
                    console.error(`\n❌ Unhandled Error: ${error.message}`);
                    if (error.stack) {
                        console.error(error.stack);
                    }
                }

                process.exit(1);
            });

            process.on('uncaughtException', (error: Error) => {
                console.error(`\n❌ Uncaught Exception: ${error.message}`);
                if (error.stack) {
                    console.error(error.stack);
                }
                process.exit(1);
            });
            """;
    }
}
