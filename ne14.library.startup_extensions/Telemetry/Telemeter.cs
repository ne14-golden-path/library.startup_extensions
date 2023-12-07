// <copyright file="Telemeter.cs" company="ne1410s">
// Copyright (c) ne1410s. All rights reserved.
// </copyright>

namespace ne14.library.startup_extensions.Telemetry;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Reflection;

/// <inheritdoc cref="ITelemeter"/>
public sealed class Telemeter : ITelemeter, IDisposable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Telemeter"/> class.
    /// </summary>
    /// <param name="assemblyName">The caller assembly name. This is exposed
    /// mainly for testing purposes.</param>
    public Telemeter(AssemblyName? assemblyName = null)
    {
        assemblyName ??= Assembly.GetCallingAssembly().GetName();
        var appName = assemblyName.Name!;
        var appVersion = assemblyName.Version?.ToString(3);

        this.AppMeter = new Meter(appName, appVersion);
        this.AppTracer = new ActivitySource(appName, appVersion);
        this.AppTags = new()
        {
            ["namespace"] = Environment.GetEnvironmentVariable("K8S_NAMESPACE"),
            ["app"] = Environment.GetEnvironmentVariable("K8S_APP"),
            ["pod"] = Environment.GetEnvironmentVariable("K8S_POD"),
        };
    }

    /// <inheritdoc/>
    public Meter AppMeter { get; }

    /// <inheritdoc/>
    public ActivitySource AppTracer { get; }

    /// <inheritdoc/>
    public ActivityTagsCollection AppTags { get; }

    /// <inheritdoc/>
    public Instrument<T> CaptureMetric<T>(
        MetricType metricType,
        T value,
        string name,
        string? unit = null,
        string? description = null,
        params KeyValuePair<string, object?>[] tags)
        where T : struct
    {
        var allTags = this.AppTags.Concat(tags).ToArray();

        switch (metricType)
        {
            case MetricType.Counter:
                var upCounter = this.AppMeter.CreateCounter<T>(name, unit, description);
                upCounter.Add(value, allTags);
                return upCounter;
            case MetricType.CounterNegatable:
                var upDownCounter = this.AppMeter.CreateUpDownCounter<T>(name, unit, description);
                upDownCounter.Add(value, allTags);
                return upDownCounter;
            case MetricType.Histogram:
                var histo = this.AppMeter.CreateHistogram<T>(name, unit, description);
                histo.Record(value, allTags);
                return histo;
            default:
                throw new ArgumentException($"Unrecognised metric type: {metricType}", nameof(metricType));
        }
    }

    /// <inheritdoc/>
    public Activity? StartTrace(
        string name,
        ActivityKind kind = ActivityKind.Internal,
        params KeyValuePair<string, object?>[] tags)
    {
        var allTags = this.AppTags.Concat(tags);
        return this.AppTracer.StartActivity(name, kind, null, allTags);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        this.AppMeter.Dispose();
        this.AppTracer.Dispose();
    }
}
