// <copyright file="RabbitMqConsumer.cs" company="ne1410s">
// Copyright (c) ne1410s. All rights reserved.
// </copyright>

namespace ne14.library.messaging.RabbitMq;

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ne14.library.messaging.Abstractions.Consumer;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

/// <inheritdoc cref="MqConsumerBase{T}"/>
public abstract class RabbitMqConsumer<T> : MqConsumerBase<T>, IDisposable
{
    private const string DefaultRoute = "DEFAULT";
    private const string Tier1Route = "T1_RETRY";
    private const string Tier2Route = "T2_DLQ";

    private readonly IConnection connection;
    private readonly IModel channel;
    private readonly AsyncEventingBasicConsumer consumer;
    private string? consumerTag;

    /// <summary>
    /// Initializes a new instance of the <see cref="RabbitMqConsumer{T}"/> class.
    /// </summary>
    /// <param name="connectionFactory">The connection factory.</param>
    protected RabbitMqConsumer(IConnectionFactory connectionFactory)
    {
        this.MessageFailed += this.OnMessageFailed;
        this.MessageProcessed += this.OnMessageProcessed;

        this.connection = connectionFactory?.CreateConnection()
            ?? throw new ArgumentNullException(nameof(connectionFactory));
        this.channel = this.connection.CreateModel();
        this.consumer = new(this.channel);
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
    protected internal override Task StartInternal(CancellationToken token)
    {
        if (this.consumerTag == null)
        {
            this.DeclareTopology();

            // Stryker disable once Assignment
            this.consumer.Received += this.OnConsumerReceipt;
            this.consumerTag = this.channel.BasicConsume(this.QueueName, false, this.consumer);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    protected internal override Task StopInternal(CancellationToken token)
    {
        // Stryker disable once Assignment
        this.consumer.Received -= this.OnConsumerReceipt;
        if (this.consumerTag != null)
        {
            this.channel.BasicCancel(this.consumerTag);
            this.consumerTag = null;
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles consumer receipt.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="args">The event args.</param>
    /// <returns>Async task.</returns>
    protected internal async Task OnConsumerReceipt(object sender, BasicDeliverEventArgs args)
    {
        args = args ?? throw new ArgumentNullException(nameof(args));
        var headers = args.BasicProperties.Headers ?? new Dictionary<string, object>();
        var attempt = headers.TryGetValue("x-attempt", out var attemptObj) ? (long?)attemptObj : null;
        var bornOn = headers.TryGetValue("x-born", out var bornObj) ? (long?)bornObj : null;
        var msgGuid = headers.TryGetValue("x-guid", out var guidObj) ? new Guid((byte[])guidObj) : Guid.NewGuid();

        var bytes = args.Body.ToArray();
        var consumerArgs = new MqConsumerEventArgs
        {
            AttemptNumber = attempt ?? 1,
            BornOn = bornOn ?? DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            DeliveryId = args.DeliveryTag,
            MessageGuid = msgGuid,
            Message = Encoding.UTF8.GetString(bytes),
        };
        await this.ConsumeInternal(args.Body.ToArray(), consumerArgs);
    }

    private void OnMessageProcessed(object? sender, MqConsumerEventArgs args)
    {
        var deliveryTag = (ulong)args.DeliveryId;
        this.channel.BasicAck(deliveryTag, false);
    }

    private void OnMessageFailed(object? sender, MqFailedEventArgs args)
    {
        var deliveryTag = (ulong)args.DeliveryId;
        if (args.Retry == false)
        {
            // NACK to DLQ
            this.channel.BasicNack(deliveryTag, false, false);
        }
        else
        {
            // ACK and republish
            var bytes = Encoding.UTF8.GetBytes(args.Message);
            const int MaxDelaySeconds = 60;
            var delayMilliseconds = Random.Shared.Next(MaxDelaySeconds * 1000);
            var props = this.channel.CreateBasicProperties();
            props.Expiration = $"{delayMilliseconds}";
            props.Headers = new Dictionary<string, object>
            {
                ["x-attempt"] = args.AttemptNumber + 1,
                ["x-born"] = args.BornOn,
                ["x-guid"] = args.MessageGuid.ToByteArray(),
            };

            this.channel.BasicAck(deliveryTag, false);
            this.channel.BasicPublish(this.ExchangeName, Tier1Route, props, bytes);
        }
    }

    private void DeclareTopology()
    {
        // Main handler queue
        var mainQArgs = new Dictionary<string, object>
        {
            ["x-dead-letter-exchange"] = this.ExchangeName,
            ["x-dead-letter-routing-key"] = Tier2Route, // NACK to DLQ
        };
        this.channel.ExchangeDeclare(this.ExchangeName, ExchangeType.Direct, true);
        this.channel.QueueDeclare(this.QueueName, true, false, false, mainQArgs);
        this.channel.QueueBind(this.QueueName, this.ExchangeName, DefaultRoute);

        // Tier 1 Failure: Retry
        var tier1Queue = this.QueueName + "_" + Tier1Route;
        var retryQArgs = new Dictionary<string, object>
        {
            ["x-dead-letter-exchange"] = this.ExchangeName,
            ["x-dead-letter-routing-key"] = DefaultRoute,
        };
        this.channel.QueueDeclare(tier1Queue, true, false, false, retryQArgs);
        this.channel.QueueBind(tier1Queue, this.ExchangeName, Tier1Route);

        // Tier 2 Failure: Dead-letter
        var tier2Queue = this.QueueName + "_" + Tier2Route;
        this.channel.QueueDeclare(tier2Queue, true, false, false);
        this.channel.QueueBind(tier2Queue, this.ExchangeName, Tier2Route);
    }
}
