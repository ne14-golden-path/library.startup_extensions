// <copyright file="ITelemeter.cs" company="ne1410s">
// Copyright (c) ne1410s. All rights reserved.
// </copyright>

namespace ne14.library.startup_extensions.Telemetry;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;

/// <summary>
/// Telemetry services.
/// </summary>
public interface ITelemeter
{
    /// <summary>
    /// Gets the meter with which the telemeter was registered.
    /// </summary>
    public Meter AppMeter { get; }

    /// <summary>
    /// Gets the activity source with which the telemeter was registered.
    /// </summary>
    public ActivitySource AppTracer { get; }

    /// <summary>
    /// Gets the base set of telemetry tags.
    /// </summary>
    public ActivityTagsCollection AppTags { get; }

    /// <summary>
    /// Captures a metric in the form of a histogram.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="metricType">The metric type.</param>
    /// <param name="value">The value.</param>
    /// <param name="name">The metric name.</param>
    /// <param name="unit">The unit of measure.</param>
    /// <param name="description">The metric description.</param>
    /// <param name="onCreate">On instrument created.</param>
    /// <param name="tags">Any tags to include.</param>
    /// <returns>The instrument.</returns>
    public Instrument<T> CaptureMetric<T>(
        MetricType metricType,
        T value,
        string name,
        string? unit = null,
        string? description = null,
        Action<Instrument>? onCreate = null,
        params KeyValuePair<string, object?>[] tags)
        where T : struct;

    /// <summary>
    /// Captures a new trace. The resulting activity must be adequately disposed of or stopped
    /// so that the trace is registered.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="kind">The kind.</param>
    /// <param name="tags">Any tags to include.</param>
    /// <returns>A new activity.</returns>
    public Activity? StartTrace(
        string name,
        ActivityKind kind = ActivityKind.Internal,
        params KeyValuePair<string, object?>[] tags);
}
