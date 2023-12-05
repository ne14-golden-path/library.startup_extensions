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
    private readonly Meter appMeter;
    private readonly ActivitySource appSource;
    private readonly ActivityTagsCollection tags;

    /// <summary>
    /// Initializes a new instance of the <see cref="Telemeter"/> class.
    /// </summary>
    public Telemeter()
    {
        var callingAssembly = Assembly.GetCallingAssembly().GetName();
        this.Name = callingAssembly.Name!;
        var appVersion = callingAssembly.Version?.ToString();

        this.appMeter = new Meter(this.Name, appVersion);
        this.appSource = new ActivitySource(this.Name, appVersion);
        this.tags = new()
        {
            ["namespace"] = Environment.GetEnvironmentVariable("K8S_NAMESPACE"),
            ["app"] = Environment.GetEnvironmentVariable("K8S_APP"),
            ["pod"] = Environment.GetEnvironmentVariable("K8S_POD"),
        };
    }

    /// <inheritdoc/>
    public string Name { get; }

    /// <inheritdoc/>
    public void CaptureMetric<T>(
        MetricType metricType,
        T value,
        string name,
        string? unit = null,
        string? description = null,
        IEnumerable<KeyValuePair<string, object?>>? tags = null)
        where T : struct
    {
        tags = this.tags.Concat(tags ?? []);

        switch (metricType)
        {
            case MetricType.Counter:
                var upCounter = this.appMeter.CreateCounter<T>(name, unit, description);
                upCounter.Add(value, tags.ToArray());
                break;
            case MetricType.CounterNegatable:
                var upDownCounter = this.appMeter.CreateUpDownCounter<T>(name, unit, description);
                upDownCounter.Add(value, tags.ToArray());
                break;
            case MetricType.Histogram:
                var histo = this.appMeter.CreateHistogram<T>(name, unit, description);
                histo.Record(value, tags.ToArray());
                break;
        }
    }

    /// <inheritdoc/>
    public Activity? StartTrace(
        string name,
        ActivityKind kind = ActivityKind.Internal,
        IEnumerable<KeyValuePair<string, object?>>? tags = null)
    {
        tags = this.tags.Concat(tags ?? []);
        return this.appSource.StartActivity(name, kind, null, tags);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        this.appMeter?.Dispose();
        this.appSource?.Dispose();
    }
}
