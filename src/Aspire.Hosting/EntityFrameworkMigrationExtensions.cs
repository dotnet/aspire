// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

/// <summary>
/// TODO:
/// </summary>
public static class EntityFrameworkMigrationExtensions
{
    /// <summary>
    /// TODO
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="builder"></param>
    /// <param name="projectMetadata"></param>
    /// <returns></returns>
    public static IResourceBuilder<T> WithEntityFrameworkMigrations<T>(this IResourceBuilder<T> builder, IProjectMetadata projectMetadata) where T: IResourceSupportsEntityFrameworkMigrations
    {
        builder.ApplicationBuilder.AddExecutable($"{builder.Resource.Name}-migrate", "dotnet", ".")
                                  .WithArgs(context => {
                                    context.Args.Add("ef");
                                    context.Args.Add("database");
                                    context.Args.Add("update");
                                    context.Args.Add("--project");
                                    context.Args.Add(projectMetadata.ProjectPath);
                                    context.Args.Add("--connection");
                                    context.Args.Add(builder);
                                  })
                                  .WaitFor((IResourceBuilder<IResource>)builder)
                                  .WithParentRelationship(builder);
        return builder;
    }
}