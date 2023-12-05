// <copyright file="ITelemeter.cs" company="ne1410s">
// Copyright (c) ne1410s. All rights reserved.
// </copyright>

namespace ne14.library.startup_extensions.Telemetry;

using System.Collections.Generic;
using System.Diagnostics;

/// <summary>
/// Telemetry services.
/// </summary>
public interface ITelemeter
{
    /// <summary>
    /// Gets the name under which the telemeter can be registered.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Captures a metric in the form of a histogram.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="metricType">The metric type.</param>
    /// <param name="value">The value.</param>
    /// <param name="name">The metric name.</param>
    /// <param name="unit">The unit of measure.</param>
    /// <param name="description">The metric description.</param>
    /// <param name="tags">Any tags to include.</param>
    public void CaptureMetric<T>(
        MetricType metricType,
        T value,
        string name,
        string? unit = null,
        string? description = null,
        IEnumerable<KeyValuePair<string, object?>>? tags = null)
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
        IEnumerable<KeyValuePair<string, object?>>? tags = null);
}
