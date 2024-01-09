﻿// <copyright file="RabbitMqConsumer.cs" company="ne1410s">
// Copyright (c) ne1410s. All rights reserved.
// </copyright>

namespace ne14.library.messaging.RabbitMq;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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

        this.connection = connectionFactory.CreateConnection();
        this.channel = this.connection.CreateModel();
        this.consumer = new(this.channel);

        // Main handler queue
        var mainQArgs = new Dictionary<string, object>
        {
            ["x-dead-letter-exchange"] = this.ExchangeName,
            ["x-dead-letter-routing-key"] = Tier1Route,
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
            var bytes = Encoding.UTF8.GetBytes(args.Message);
            this.channel.BasicAck(deliveryTag, false);
            this.channel.BasicPublish(this.ExchangeName, Tier2Route, null, bytes);
        }
        else
        {
            this.channel.BasicNack(deliveryTag, false, false);
        }
    }

    [ExcludeFromCodeCoverage]
    private async Task OnConsumerReceipt(object sender, BasicDeliverEventArgs args)
    {
        var attempt = 1L;
        var bornOn = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var headers = args.BasicProperties.Headers ?? new Dictionary<string, object>();
        if (headers.TryGetValue("x-death", out var death) && death is List<object> deathList)
        {
            var dicto = (Dictionary<string, object>)deathList[0];
            attempt = dicto.TryGetValue("count", out var countObj) ? (long)countObj : attempt;
            bornOn = dicto.TryGetValue("time", out var timeObj) ? ((AmqpTimestamp)timeObj).UnixTime : bornOn;
        }

        var bytes = args.Body.ToArray();
        var consumerArgs = new MqConsumerEventArgs
        {
            AttemptNumber = attempt,
            BornOn = bornOn,
            DeliveryId = args.DeliveryTag,
            Message = Encoding.UTF8.GetString(bytes),
        };
        await this.ConsumeInternal(args.Body.ToArray(), consumerArgs);
    }
}