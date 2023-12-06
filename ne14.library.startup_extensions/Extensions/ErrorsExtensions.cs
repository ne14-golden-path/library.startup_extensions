// <copyright file="ErrorsExtensions.cs" company="ne1410s">
// Copyright (c) ne1410s. All rights reserved.
// </copyright>

namespace ne14.library.startup_extensions;

using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using ne14.library.fluent_errors.Errors;
using ne14.library.fluent_errors.Validation;
using ne14.library.startup_extensions.Errors;

/// <summary>
/// Extensions relating to error handling.
/// </summary>
public static class ErrorsExtensions
{
    /// <summary>
    /// Adds FluentErrors.
    /// </summary>
    /// <param name="services">The services.</param>
    /// <returns>The service collection.</returns>
    /// <exception cref="StaticValidationException">Model error.</exception>
    public static IServiceCollection AddEnterpriseErrorHandling(
        this IServiceCollection services)
    {
        return services.Configure<ApiBehaviorOptions>(opts =>
        {
            opts.InvalidModelStateResponseFactory = ctx =>
            {
                var invalidItems = ctx.ModelState.ToItems();
                throw new StaticValidationException(invalidItems);
            };
        });
    }

    /// <summary>
    /// Uses the FluentErrors feature.
    /// </summary>
    /// <param name="app">The app builder.</param>
    /// <returns>The same app builder.</returns>
    public static IApplicationBuilder UseEnterpriseErrorHandling(
        this IApplicationBuilder app)
    {
        return app.UseMiddleware<FluentErrorsMiddleware>();
    }

    private static InvalidItem[] ToItems(this ModelStateDictionary state)
        => state.Select(e => new InvalidItem(
            e.Key,
            string.Join(", ", e.Value!.Errors.Select(m => m.ErrorMessage)),
            e.Value.RawValue)).ToArray();
}
