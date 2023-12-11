// <copyright file="CorsExtensions.cs" company="ne1410s">
// Copyright (c) ne1410s. All rights reserved.
// </copyright>

namespace ne14.library.startup_extensions;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extensions relating to cors.
/// </summary>
public static class CorsExtensions
{
    /// <summary>
    /// Adds the enterprise cors feature.
    /// </summary>
    /// <param name="services">The services.</param>
    /// <param name="origins">The allowed origins.</param>
    /// <param name="headers">The allowed headers.</param>
    /// <returns>The original parameter, for chainable commands.</returns>
    public static IServiceCollection AddEnterpriseCors(
        this IServiceCollection services,
        string[]? origins = null,
        string[]? headers = null)
    {
        headers = ["Authorization", "Content-Type", .. headers ?? []];
        return services.AddCors(o => o
            .AddPolicy(nameof(startup_extensions), builder => builder
                .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE")
                .WithHeaders(headers)
                .WithOrigins(origins?.Length > 0 ? origins : ["*"])
                .SetIsOriginAllowedToAllowWildcardSubdomains()));
    }

    /// <summary>
    /// Uses the enterprise cors feature.
    /// </summary>
    /// <param name="app">The app builder.</param>
    /// <returns>The original parameter, for chainable commands.</returns>
    public static IApplicationBuilder UseEnterpriseCors(
        this IApplicationBuilder app)
    {
        return app.UseCors(nameof(startup_extensions));
    }
}
