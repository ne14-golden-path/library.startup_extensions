// <copyright file="MqFailedEventArgs.cs" company="ne1410s">
// Copyright (c) ne1410s. All rights reserved.
// </copyright>

namespace ne14.library.messaging.Abstractions.Consumer;

using System;

/// <summary>
/// Event args when mq message handling fails.
/// </summary>
public class MqFailedEventArgs : MqConsumerEventArgs
{
    /// <summary>
    /// Gets the error.
    /// </summary>
    public Exception Error { get; init; } = default!;

    /// <summary>
    /// Gets or sets a value indicating whether to retry.
    /// </summary>
    public bool? Retry { get; set; }
}
