// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Aspire;

internal static class EntityFrameworkUtils
{
    /// <summary>
    /// Enriches the DbContext options service descriptor with custom alterations.
    /// </summary>
    public static void PatchServiceDescriptor<TContext>(this IHostApplicationBuilder builder, Action<DbContextOptionsBuilder<TContext>>? options = null, [CallerMemberName] string memberName = "")
        where TContext : DbContext
    {
        // Resolving DbContext<TContextService> will resolve DbContextOptions<TContextImplementation>.
        // We need to replace the DbContextOptions service descriptor to inject more logic. This won't be necessary once
        // Aspire targets .NET 9 as EF will respect the calls to services.ConfigureDbContext<TContext>(). c.f. https://github.com/dotnet/efcore/pull/32518

        var oldDbContextOptionsDescriptor = builder.Services.FirstOrDefault(sd => sd.ServiceType == typeof(DbContextOptions<TContext>));

        if (oldDbContextOptionsDescriptor is null)
        {
            throw new InvalidOperationException($"DbContext<{typeof(TContext).Name}> was not registered. Ensure you have registered the DbContext in DI before calling {memberName}.");
        }

        if (options == null)
        {
            return;
        }

        builder.Services.Remove(oldDbContextOptionsDescriptor);

        var dbContextOptionsDescriptor = new ServiceDescriptor(
            oldDbContextOptionsDescriptor.ServiceType,
            oldDbContextOptionsDescriptor.ServiceKey,
            factory: (sp, key) =>
            {
                var dbContextOptions = oldDbContextOptionsDescriptor.ImplementationFactory?.Invoke(sp) as DbContextOptions<TContext>;

                if (dbContextOptions is null)
                {
                    throw new InvalidOperationException($"DbContext<{typeof(TContext).Name}> was not registered. Ensure you have registered the DbContext in DI before calling {memberName}.");
                }

                var optionsBuilder = dbContextOptions != null
                    ? new DbContextOptionsBuilder<TContext>(dbContextOptions)
                    : new DbContextOptionsBuilder<TContext>();

                options(optionsBuilder);

                return optionsBuilder.Options;
            },
            oldDbContextOptionsDescriptor.Lifetime
            );

        builder.Services.Add(dbContextOptionsDescriptor);
    }
}
