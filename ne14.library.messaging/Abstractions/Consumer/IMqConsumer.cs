// <copyright file="IMqConsumer.cs" company="ne1410s">
// Copyright (c) ne1410s. All rights reserved.
// </copyright>

namespace ne14.library.messaging.Abstractions.Consumer;

using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

/// <summary>
/// A hosted service that consumes mq messages.
/// </summary>
public interface IMqConsumer : IHostedService
{
    /// <summary>
    /// Gets the number of times the consumer will attempt processing before abandonment.
    /// </summary>
    public long? MaximumAttempts { get; }

    /// <summary>
    /// Gets the consumer app name.
    /// </summary>
    public string? ConsumerAppName { get; }

    /// <summary>
    /// Gets the exchange name.
    /// </summary>
    public string ExchangeName { get; }

    /// <summary>
    /// Gets the queue name.
    /// </summary>
    public string QueueName { get; }
}

/// <summary>
/// A hosted service that consumes mq messages of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
public interface IMqConsumer<in T> : IMqConsumer
{
    /// <summary>
    /// Consumes a message.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <param name="args">The event args.</param>
    /// <returns>Async task.</returns>
    public Task Consume(T message, MqConsumerEventArgs args);
}