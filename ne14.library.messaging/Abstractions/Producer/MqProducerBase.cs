// <copyright file="MqProducerBase.cs" company="ne1410s">
// Copyright (c) ne1410s. All rights reserved.
// </copyright>

namespace ne14.library.messaging.Abstractions.Producer;

using System;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ne14.library.messaging.Abstractions.Consumer;

/// <summary>
/// Base implementation for producing mq messages.
/// </summary>
public abstract class MqProducerBase : IMqProducer
{
    /// <inheritdoc/>
    public abstract string ExchangeName { get; }
}

/// <inheritdoc cref="IMqProducer{T}"/>
public abstract class MqProducerBase<T> : MqProducerBase, IMqProducer<T>
{
    private readonly JsonSerializerOptions jsonOpts = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    /// <summary>
    /// Fires when a message is about to be sent.
    /// </summary>
    public event EventHandler<MqEventArgs>? MessageSending;

    /// <summary>
    /// Fires when a message has been sent.
    /// </summary>
    public event EventHandler<MqEventArgs>? MessageSent;

    /// <inheritdoc/>
    public void Produce(T message)
    {
        var json = JsonSerializer.Serialize(message, this.jsonOpts);
        var args = new MqEventArgs { Message = json };
        this.MessageSending?.Invoke(this, args);
        this.ProduceInternal(Encoding.UTF8.GetBytes(json));
        this.MessageSent?.Invoke(this, args);
    }

    /// <summary>
    /// Produces the message internally.
    /// </summary>
    /// <param name="bytes">The message bytes.</param>
    protected internal abstract void ProduceInternal(byte[] bytes);
}