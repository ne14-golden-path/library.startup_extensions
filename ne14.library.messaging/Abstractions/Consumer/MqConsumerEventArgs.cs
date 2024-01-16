// <copyright file="MqConsumerEventArgs.cs" company="ne1410s">
// Copyright (c) ne1410s. All rights reserved.
// </copyright>

namespace ne14.library.messaging.Abstractions.Consumer;

using System;

/// <summary>
/// Mq consumer event arguments.
/// </summary>
public class MqConsumerEventArgs : MqEventArgs
{
    /// <summary>
    /// Gets the Epoch time the message was first received.
    /// </summary>
    public long BornOn { get; init; }

    /// <summary>
    /// Gets the attempt number.
    /// </summary>
    public long AttemptNumber { get; init; }

    /// <summary>
    /// Gets the message guid.
    /// </summary>
    public Guid MessageGuid { get; init; }

    /// <summary>
    /// Gets the delivery identifier.
    /// </summary>
    public object DeliveryId { get; init; } = default!;
}
