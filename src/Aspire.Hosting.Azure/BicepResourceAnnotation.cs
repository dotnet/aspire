// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Used to annotate resources as being potentially deployable by the <see cref="AzureProvisioner"/>.
/// </summary>
/// <param name="resource">The <see cref="AzureBicepResource"/> which should be used by the <see cref="AzureProvisioner"/>.</param>
/// <remarks>
///     <para>
///         The <see cref="AzureProvisioner"/> is only capable of deploying resources that implement <see cref="IAzureResource"/>
///         and only has built-in deployment logic for resources that derive from <see cref="AzureBicepResource"/>. This annotation
///         that can be added to any <see cref="IResource"/> will be detected by the <see cref="AzureProvisioner"/> and used to
///         provision an Azure resource for an Aspire resource type that does not itself derive from <see cref="AzureBicepResource"/>.
///     </para>
///     <para>
///         For example, the following code adds a <see href="https://learn.microsoft.com/dotnet/api/aspire.hosting.applicationmodel.sqlserverserverresource"/>
///         resource to the application model. This type does not derive from <see cref="AzureBicepResource"/> but can be annotated with
///         <see cref="AzureBicepResourceAnnotation"/> by using the AzureSqlExtensions.AsAzureSqlDatabase() extension method.
///     </para>
///     <code lang="csharp">
///         var builder = DistributedApplication.CreateBuilder();
///         builder.AddAzureProvisioning();
///         var sql = builder.AddSqlServerServer("sql"); // This resource would not be deployable via Azure Provisioner.
///         sql.AsAzureSqlDatabase(); // ... but it now is because this adds the AzureBicepResourceAnnotation annotation.
///     </code>
/// </remarks>
public class AzureBicepResourceAnnotation(AzureBicepResource resource) : IResourceAnnotation
{
    /// <summary>
    /// The <see cref="AzureBicepResource"/> derived resource.
    /// </summary>
    public AzureBicepResource Resource => resource;
}
