// <copyright file="DiscoveryExtensions.cs" company="ne1410s">
// Copyright (c) ne1410s. All rights reserved.
// </copyright>

namespace ne14.library.startup_extensions;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

/// <summary>
/// Extensions relating to discovery / swagger.
/// </summary>
public static class DiscoveryExtensions
{
    /// <summary>
    /// Adds the enterprise discovery feature.
    /// </summary>
    /// <param name="services">The services.</param>
    /// <returns>The original parameter, for chainable commands.</returns>
    public static IServiceCollection AddEnterpriseDiscovery(
    this IServiceCollection services)
    {
        return services
            .AddEndpointsApiExplorer()
            .AddSwaggerGen();
    }

    /// <summary>
    /// Uses the enterprise discovery feature.
    /// </summary>
    /// <param name="app">The app builder.</param>
    /// <param name="environment">The environment.</param>
    /// <returns>The original parameter, for chainable commands.</returns>
    public static IApplicationBuilder UseEnterpriseDiscovery(
        this IApplicationBuilder app,
        IWebHostEnvironment environment)
    {
        if (environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        return app;
    }
}
