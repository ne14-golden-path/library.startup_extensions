// <copyright file="TelemetryExtensions.cs" company="ne1410s">
// Copyright (c) ne1410s. All rights reserved.
// </copyright>

namespace ne14.library.startup_extensions.Extensions;

using System;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ne14.library.startup_extensions.Telemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

/// <summary>
/// Extensions relating to telemetry.
/// </summary>
public static class TelemetryExtensions
{
    /// <summary>
    /// Adds the enterprise telemetry feature.
    /// </summary>
    /// <param name="services">The services.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The original parameter, for chainable commands.</returns>
    public static IServiceCollection AddEnterpriseTelemetry(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var callingAssembly = Assembly.GetCallingAssembly().GetName();
        var appName = callingAssembly.Name!;

        var appResourceBuilder = ResourceBuilder
            .CreateDefault()
            .AddTelemetrySdk()
            .AddService(appName)
            .AddEnvironmentVariableDetector();

        void OpenTelemetryOptsBuilder(OtlpExporterOptions opts)
        {
            opts.Protocol = OtlpExportProtocol.Grpc;
            opts.Endpoint = new Uri(configuration["OpenTel:Grpc"]!);
        }

        services.AddSingleton<ITelemeter, Telemeter>();

        services.AddOpenTelemetry()
            .WithTracing(builder => builder
                .SetResourceBuilder(appResourceBuilder)
                .AddSource(appName)
                .AddAspNetCoreInstrumentation(opts => opts.RecordException = true)
                .AddHttpClientInstrumentation()
                .AddEntityFrameworkCoreInstrumentation()
                .AddOtlpExporter(OpenTelemetryOptsBuilder))
            .WithMetrics(builder => builder
                .SetResourceBuilder(appResourceBuilder)
                .AddMeter(appName)
                .AddRuntimeInstrumentation()
                .AddAspNetCoreInstrumentation()
                .AddOtlpExporter(OpenTelemetryOptsBuilder));

        return services;
    }
}
