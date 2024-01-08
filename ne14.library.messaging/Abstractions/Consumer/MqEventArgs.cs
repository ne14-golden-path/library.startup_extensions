// <copyright file="MqEventArgs.cs" company="ne1410s">
// Copyright (c) ne1410s. All rights reserved.
// </copyright>

namespace ne14.library.messaging.Abstractions.Consumer;

using System;

/// <summary>
/// Mq message event args.
/// </summary>
public class MqEventArgs : EventArgs
{
    /// <summary>
    /// Gets the original message.
    /// </summary>
    public string Message { get; init; } = default!;
}
