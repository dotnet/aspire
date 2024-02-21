// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Aspire;

internal static class EntityFrameworkUtils
{
    /// <summary>
    /// Binds the DbContext specific configuration section to settings when available.
    /// </summary>
    public static TSettings GetDbContextSettings<TContext, TSettings>(this IHostApplicationBuilder builder, string defaultConfigSectionName, Action<TSettings, IConfiguration> bindSettings)
        where TSettings : new()
    {
        TSettings settings = new();
        var typeSpecificSectionName = $"{defaultConfigSectionName}:{typeof(TContext).Name}";
        var typeSpecificConfigurationSection = builder.Configuration.GetSection(typeSpecificSectionName);
        if (typeSpecificConfigurationSection.Exists()) // https://github.com/dotnet/runtime/issues/91380
        {
            bindSettings(settings, typeSpecificConfigurationSection);
        }
        else
        {
            var section = builder.Configuration.GetSection(defaultConfigSectionName);
            bindSettings(settings, section);
        }

        return settings;
    }

    /// <summary>
    /// Enriches the DbContext options service descriptor with custom alterations.
    /// </summary>
    public static void PatchServiceDescriptor<TContext>(this IHostApplicationBuilder builder, Action<DbContextOptionsBuilder<TContext>>? configureDbContextOptions = null, [CallerMemberName] string memberName = "")
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

        if (configureDbContextOptions == null)
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
                    throw new InvalidOperationException($"DbContext<{typeof(TContext).Name}> was not configured. Ensure you have registered the DbContext in DI before calling {memberName}.");
                }

                var optionsBuilder = dbContextOptions != null
                    ? new DbContextOptionsBuilder<TContext>(dbContextOptions)
                    : new DbContextOptionsBuilder<TContext>();

                configureDbContextOptions(optionsBuilder);

                return optionsBuilder.Options;
            },
            oldDbContextOptionsDescriptor.Lifetime
            );

        builder.Services.Add(dbContextOptionsDescriptor);
    }
}
