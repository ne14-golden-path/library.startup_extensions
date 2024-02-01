﻿// <copyright file="TestObjects.cs" company="ne1410s">
// Copyright (c) ne1410s. All rights reserved.
// </copyright>

using ne14.library.messaging.Abstractions.Consumer;
using ne14.library.messaging.RabbitMq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ne14.library.messaging.tests;

/// <summary>
/// A basic payload.
/// </summary>
/// <param name="PermaFail">Whether to fail permanently.</param>
public record BasicPayload(bool? PermaFail);

/// <summary>
/// A basic producer.
/// </summary>
/// <param name="factory">The connection factory.</param>
public class BasicProducer(IConnectionFactory factory)
    : RabbitMqProducer<BasicPayload>(factory)
{
    public override string ExchangeName => TestHelper.TestExchangeName;
}

public class BasicConsumer(IConnectionFactory factory)
    : RabbitMqConsumer<BasicPayload>(factory)
{
    public override string ExchangeName => TestHelper.TestExchangeName;

    public override Task ConsumeAsync(BasicPayload message, MqConsumerEventArgs args)
    {
        return message.PermaFail switch
        {
            true => throw new PermanentFailureException(),
            false => throw new TransientFailureException(),
            _ => Task.CompletedTask,
        };
    }

    public async Task TestConsumerReceipt(object sender, BasicDeliverEventArgs args)
        => await this.OnConsumerReceipt(sender, args);
}