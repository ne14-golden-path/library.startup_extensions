// <copyright file="MetricType.cs" company="ne1410s">
// Copyright (c) ne1410s. All rights reserved.
// </copyright>

namespace ne14.library.startup_extensions.Telemetry;

/// <summary>
/// Supported metric type.
/// </summary>
public enum MetricType
{
    /// <summary>
    /// Counter (purely additive). Example: bytes received.
    /// </summary>
    Counter,

    /// <summary>
    /// Counter (may increase or decrease). Example: current queue size.
    /// </summary>
    CounterNegatable,

    /// <summary>
    /// Histogram (statistical). Example: request duration.
    /// </summary>
    Histogram,
}
