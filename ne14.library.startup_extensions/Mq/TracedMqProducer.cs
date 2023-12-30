// <copyright file="TracedMqProducer.cs" company="ne1410s">
// Copyright (c) ne1410s. All rights reserved.
// </copyright>

namespace ne14.library.startup_extensions.Mq;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ne14.library.rabbitmq.Vendor;
using ne14.library.startup_extensions.Telemetry;

/// <inheritdoc/>
public abstract class TracedMqProducer<T> : RabbitMqProducer<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TracedMqProducer{T}"/> class.
    /// </summary>
    /// <param name="session">The session.</param>
    /// <param name="telemeter">The telemeter.</param>
    /// <param name="logger">The logger.</param>
    protected TracedMqProducer(
        IRabbitMqSession session,
        ITelemeter telemeter,
        ILogger<TracedMqProducer<T>> logger)
        : base(session)
    {
        this.Telemeter = telemeter;
        this.Logger = logger;
    }

    /// <summary>
    /// Gets the telemeter.
    /// </summary>
    protected ITelemeter Telemeter { get; }

    /// <summary>
    /// Gets the logger.
    /// </summary>
    protected ILogger<TracedMqProducer<T>> Logger { get; }

    /// <inheritdoc/>
    protected override Task OnProducing(string message)
    {
        this.Logger.LogInformation("Mq message sending: {Exchange}", this.ExchangeName);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    protected override Task OnProduced(string message)
    {
        this.Logger.LogInformation("Mq message sent: {Exchange}", this.ExchangeName);

        var tags = new Dictionary<string, object?>()
        {
            ["exchange"] = this.ExchangeName,
            ["json"] = message,
        };

        this.Telemeter.CaptureMetric(MetricType.Counter, 1, "mq-produce", tags: tags.ToArray());
        using var activity = this.Telemeter.StartTrace("mq-produce", tags: tags.ToArray());
        return Task.CompletedTask;
    }
}
