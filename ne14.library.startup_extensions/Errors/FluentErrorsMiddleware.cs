// <copyright file="FluentErrorsMiddleware.cs" company="ne1410s">
// Copyright (c) ne1410s. All rights reserved.
// </copyright>

namespace ne14.library.startup_extensions.Errors;

using System;
using System.Threading.Tasks;
using FluentErrors.Api;
using FluentErrors.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

/// <summary>
/// Middleware for fluent errors.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="FluentErrorsMiddleware"/> class.
/// </remarks>
/// <param name="next">The request delegate.</param>
/// <param name="logger">The logger.</param>
public class FluentErrorsMiddleware(
    RequestDelegate next,
    ILogger<FluentErrorsMiddleware> logger)
{
    /// <summary>
    /// Invokes the middleware.
    /// </summary>
    /// <param name="context">The http context.</param>
    /// <returns>Asynchronous task.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        context.MustExist();

        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception");
            var httpOutcome = ex.ToOutcome();
            context.Response.StatusCode = httpOutcome.ErrorCode;
            await context.Response.WriteAsJsonAsync(httpOutcome.ErrorBody);
        }
    }
}
