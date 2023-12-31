﻿// <copyright file="TracedMqConsumer.cs" company="ne1410s">
// Copyright (c) ne1410s. All rights reserved.
// </copyright>

namespace ne14.library.startup_extensions.Mq;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ne14.library.fluent_errors.Extensions;
using ne14.library.rabbitmq.Consumer;
using ne14.library.rabbitmq.Vendor;
using ne14.library.startup_extensions.Telemetry;

/// <inheritdoc/>
public abstract class TracedMqConsumer<T> : RabbitMqConsumer<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TracedMqConsumer{T}"/> class.
    /// </summary>
    /// <param name="session">The session.</param>
    /// <param name="telemeter">The telemeter.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="config">The config.</param>
    protected TracedMqConsumer(
        IRabbitMqSession session,
        ITelemeter telemeter,
        ILogger<TracedMqConsumer<T>> logger,
        IConfiguration config)
        : base(session)
    {
        this.Telemeter = telemeter;
        this.Logger = logger;
        this.MaxAttempts = this.GetConfig<long>(config, nameof(this.MaxAttempts));
    }

    /// <summary>
    /// Gets the telemeter.
    /// </summary>
    protected ITelemeter Telemeter { get; }

    /// <summary>
    /// Gets the logger.
    /// </summary>
    protected ILogger<TracedMqConsumer<T>> Logger { get; }

    /// <inheritdoc/>
    protected override Task OnServiceStarting()
    {
        this.Logger.LogInformation("Mq consumer starting: {Queue}", this.QueueName);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    protected override Task OnServiceStarted()
    {
        this.Logger.LogInformation("Mq consumer started: {Queue}", this.QueueName);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    protected override Task OnServiceStopping()
    {
        this.Logger.LogInformation("Mq consumer stopping: {Queue}", this.QueueName);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    protected override Task OnServiceStopped()
    {
        this.Logger.LogInformation("Mq consumer stopped: {Queue}", this.QueueName);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    protected override Task OnConsuming(string json, ConsumerContext context)
    {
        context.MustExist();
        this.Logger.LogInformation(
            "Mq message incoming: {Queue}@{BornOn} ({Attempt}x)",
            this.QueueName,
            context.BornOn,
            context.AttemptNumber);

        var tags = new Dictionary<string, object?>()
        {
            ["queue"] = this.QueueName,
            ["born"] = context.BornOn,
            ["json"] = json,
            ["attempt"] = context.AttemptNumber,
        };

        using var activity = this.Telemeter.StartTrace("mq-consume", tags: tags.ToArray());
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    protected override async Task OnConsumeSuccess(string json, ConsumerContext context)
    {
        context.MustExist();
        await base.OnConsumeSuccess(json, context);

        this.Logger.LogInformation(
            "Mq message success: {Queue}#{BornOn} ({Attempt}x)",
            this.QueueName,
            context.BornOn,
            context.AttemptNumber);

        var tags = new Dictionary<string, object?>()
        {
            ["queue"] = this.QueueName,
        };
        this.Telemeter.CaptureMetric(MetricType.Counter, 1, "mq-consume-success", tags: tags.ToArray());
    }

    /// <inheritdoc/>
    protected override async Task OnConsumeFailure(string json, ConsumerContext context, bool retry)
    {
        context.MustExist();
        await base.OnConsumeFailure(json, context, retry);

        this.Logger.Log(
            retry ? LogLevel.Warning : LogLevel.Error,
            "Mq {FailMode} failure: {Queue}#{BornOn} ({Attempt}x)",
            retry ? "transient" : "permanent",
            this.QueueName,
            context.BornOn,
            context.AttemptNumber);

        var tags = new Dictionary<string, object?>()
        {
            ["outcome"] = retry ? "retry" : "abort",
            ["queue"] = this.QueueName,
        };
        this.Telemeter.CaptureMetric(MetricType.Counter, 1, "mq-consume-failure", tags: tags.ToArray());
    }

    private TProp? GetConfig<TProp>(IConfiguration config, string property)
        where TProp : struct
    {
        var queueValue = config.GetValue<TProp?>($"RabbitMq:Queues:{this.QueueName}:{property}");
        var exchangeValue = config.GetValue<TProp?>($"RabbitMq:Exchanges:{this.ExchangeName}:{property}");
        return queueValue ?? exchangeValue;
    }
}