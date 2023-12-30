// <copyright file="TestObjects.cs" company="ne1410s">
// Copyright (c) ne1410s. All rights reserved.
// </copyright>

namespace ne14.library.startup_extensions.tests.Mq;

using Microsoft.Extensions.Logging;
using ne14.library.rabbitmq.Consumer;
using ne14.library.rabbitmq.Exceptions;
using ne14.library.rabbitmq.Vendor;
using ne14.library.startup_extensions.Mq;
using ne14.library.startup_extensions.Telemetry;

public record BasicPayload(string Foo, bool? SimulateRetry);

public class BasicTracedProducer(
    IRabbitMqSession session,
    ITelemeter telemeter,
    ILogger<TracedMqProducer<BasicPayload>> logger)
        : TracedMqProducer<BasicPayload>(session, telemeter, logger)
{
    public override string ExchangeName => "basic-thing";
}

public class BasicTracedConsumer(
    IRabbitMqSession session,
    ITelemeter telemeter,
    ILogger<TracedMqConsumer<BasicPayload>> logger)
        : TracedMqConsumer<BasicPayload>(session, telemeter, logger)
{
    public override string ExchangeName => "basic-thing";

    public override Task Consume(BasicPayload message, ConsumerContext context)
    {
        return message.SimulateRetry switch
        {
            true => throw new TransientFailureException(),
            false => throw new PermanentFailureException(),
            _ => Task.CompletedTask,
        };
    }

    public async Task TestOnConsumeSuccess(string json, ConsumerContext context)
        => await this.OnConsumeSuccess(json, context);

    public async Task TestOnConsumeFailure(string json, ConsumerContext context,  bool retry)
        => await this.OnConsumeFailure(json, context, retry);
}
