// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Eventing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

// ============================================================================
// ATS Type Mappings for Aspire.Hosting
// ============================================================================
// These assembly-level attributes define the CLR type → ATS type ID mappings.
// This centralizes all type mapping logic and eliminates inference/string parsing.

// Core types
[assembly: AspireExport(typeof(IDistributedApplicationBuilder), AtsTypeId = "aspire/Builder")]
[assembly: AspireExport(typeof(DistributedApplication), AtsTypeId = "aspire/Application")]
[assembly: AspireExport(typeof(DistributedApplicationExecutionContext), AtsTypeId = "aspire/ExecutionContext")]

// Reference types
[assembly: AspireExport(typeof(EndpointReference), AtsTypeId = "aspire/EndpointReference")]
[assembly: AspireExport(typeof(ReferenceExpression), AtsTypeId = "aspire/ReferenceExpression")]

// Callback context types
[assembly: AspireExport(typeof(EnvironmentCallbackContext), AtsTypeId = "aspire/EnvironmentContext")]

// Resource interfaces
[assembly: AspireExport(typeof(IResource), AtsTypeId = "aspire/IResource")]
[assembly: AspireExport(typeof(IResourceWithEnvironment), AtsTypeId = "aspire/IResourceWithEnvironment")]
[assembly: AspireExport(typeof(IResourceWithEndpoints), AtsTypeId = "aspire/IResourceWithEndpoints")]
[assembly: AspireExport(typeof(IResourceWithArgs), AtsTypeId = "aspire/IResourceWithArgs")]
[assembly: AspireExport(typeof(IResourceWithConnectionString), AtsTypeId = "aspire/IResourceWithConnectionString")]
[assembly: AspireExport(typeof(IResourceWithWaitSupport), AtsTypeId = "aspire/IResourceWithWaitSupport")]
[assembly: AspireExport(typeof(IResourceWithParent), AtsTypeId = "aspire/IResourceWithParent")]

// Concrete resource types
[assembly: AspireExport(typeof(ContainerResource), AtsTypeId = "aspire/Container")]
[assembly: AspireExport(typeof(ExecutableResource), AtsTypeId = "aspire/Executable")]
[assembly: AspireExport(typeof(ProjectResource), AtsTypeId = "aspire/Project")]
[assembly: AspireExport(typeof(ParameterResource), AtsTypeId = "aspire/Parameter")]

// Service types
[assembly: AspireExport(typeof(IServiceProvider), AtsTypeId = "aspire/ServiceProvider")]
[assembly: AspireExport(typeof(ResourceNotificationService), AtsTypeId = "aspire/ResourceNotificationService")]
[assembly: AspireExport(typeof(ResourceLoggerService), AtsTypeId = "aspire/ResourceLoggerService")]

// External types we reference
[assembly: AspireExport(typeof(IConfiguration), AtsTypeId = "aspire/Configuration")]
[assembly: AspireExport(typeof(IHostEnvironment), AtsTypeId = "aspire/HostEnvironment")]
[assembly: AspireExport(typeof(CancellationToken), AtsTypeId = "aspire/CancellationToken")]

// Eventing types
[assembly: AspireExport(typeof(DistributedApplicationEventSubscription), AtsTypeId = "aspire/EventSubscription")]
