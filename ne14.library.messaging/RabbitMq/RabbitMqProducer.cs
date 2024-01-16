// <copyright file="RabbitMqProducer.cs" company="ne1410s">
// Copyright (c) ne1410s. All rights reserved.
// </copyright>

namespace ne14.library.messaging.RabbitMq;

using System;
using System.Collections.Generic;
using ne14.library.messaging.Abstractions.Producer;
using RabbitMQ.Client;

/// <inheritdoc cref="MqProducerBase{T}"/>
public abstract class RabbitMqProducer<T> : MqProducerBase<T>, IDisposable
{
    private const string DefaultRoute = "DEFAULT";

    private readonly IConnection connection;
    private readonly IModel channel;

    /// <summary>
    /// Initializes a new instance of the <see cref="RabbitMqProducer{T}"/> class.
    /// </summary>
    /// <param name="connectionFactory">The connection factory.</param>
    protected RabbitMqProducer(IConnectionFactory connectionFactory)
    {
        this.connection = connectionFactory.CreateConnection();
        this.channel = this.connection.CreateModel();
        this.channel.ExchangeDeclare(this.ExchangeName, ExchangeType.Direct, true);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        this.channel.Close();
        this.connection.Close();
        this.connection.Dispose();
    }

    /// <inheritdoc/>
    protected internal override void ProduceInternal(byte[] bytes)
    {
        var props = this.channel.CreateBasicProperties();
        props.Headers = new Dictionary<string, object>
        {
            ["x-attempt"] = 1L,
            ["x-born"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            ["x-guid"] = Guid.NewGuid().ToByteArray(),
        };

        this.channel.BasicPublish(this.ExchangeName, DefaultRoute, props, bytes);
    }
}
