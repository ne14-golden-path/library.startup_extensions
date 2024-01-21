// <copyright file="TestObjects.cs" company="ne1410s">
// Copyright (c) ne1410s. All rights reserved.
// </copyright>

namespace ne14.library.startup_extensions.tests.Mq;

using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ne14.library.messaging.Abstractions.Consumer;
using ne14.library.startup_extensions.Mq;
using ne14.library.startup_extensions.Telemetry;
using RabbitMQ.Client;

public record BasicPayload(string Foo, bool? SimulateRetry);

public class BasicTracedProducer(
    IConnectionFactory connectionFactory,
    ITelemeter telemeter,
    ILogger<BasicTracedProducer> logger)
        : MqTracingProducer<BasicPayload>(connectionFactory, telemeter, logger)
{
    public override string ExchangeName => "basic-thing";
}

public class BasicTracedConsumer(
    IConnectionFactory connectionFactory,
    ITelemeter telemeter,
    ILogger<BasicTracedConsumer> logger,
    IConfiguration config)
        : MqTracingConsumer<BasicPayload>(connectionFactory, telemeter, logger, config)
{
    public override string ExchangeName => "basic-thing";

    public override Task ConsumeAsync(BasicPayload message, MqConsumerEventArgs args)
    {
        return message.SimulateRetry switch
        {
            true => throw new TransientFailureException(),
            false => throw new PermanentFailureException(),
            _ => Task.CompletedTask,
        };
    }
}

public static class TextExtensions
{
    public static void FireEvent<T>(
        this T source,
        string eventName,
        EventArgs? args = null)
    {
        var multiDelegate = typeof(T)
            .GetField(eventName, BindingFlags.Instance | BindingFlags.NonPublic)?
            .GetValue(source) as MulticastDelegate;

        foreach (var dlg in multiDelegate!.GetInvocationList())
        {
            dlg.Method.Invoke(dlg.Target, [null, args ?? EventArgs.Empty]);
        }
    }
}