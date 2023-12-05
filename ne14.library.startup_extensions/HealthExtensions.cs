// <copyright file="HealthExtensions.cs" company="ne1410s">
// Copyright (c) ne1410s. All rights reserved.
// </copyright>

namespace ne14.library.startup_extensions;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extensions relating to health / health checks.
/// </summary>
public static class HealthExtensions
{
    /// <summary>
    /// Adds the enterprise health feature.
    /// </summary>
    /// <param name="services">The services.</param>
    /// <returns>The health checks builder, for chaining additional checks.</returns>
    public static IHealthChecksBuilder AddEnterpriseHealthChecks(
        this IServiceCollection services)
    {
        return services.AddHealthChecks();
    }

    /// <summary>
    /// Uses the enterprise health feature.
    /// </summary>
    /// <typeparam name="T">The app builder type.</typeparam>
    /// <param name="app">The endpoint route builder.</param>
    /// <returns>The original parameter, for chainable commands.</returns>
    public static T UseEnterpriseHealthChecks<T>(this T app)
        where T : IApplicationBuilder, IEndpointRouteBuilder
    {
        // Start: include all dependencies
        app.MapHealthChecks("healthz");

        // Ready: exclude non-vital dependencies
        app.MapHealthChecks("healthz/ready", new() { Predicate = c => !c.Tags.Contains("non-vital") });

        // Live: exclude all dependencies
        app.MapHealthChecks("healthz/live", new() { Predicate = _ => false });

        return app;
    }
}
