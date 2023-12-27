// <copyright file="TracedMqConsumer.cs" company="ne1410s">
// Copyright (c) ne1410s. All rights reserved.
// </copyright>

namespace ne14.library.startup_extensions.Mq;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ne14.library.rabbitmq;
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
    protected TracedMqConsumer(
        RabbitMqSession session,
        ITelemeter telemeter,
        ILogger<TracedMqConsumer<T>> logger)
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
    protected ILogger<TracedMqConsumer<T>> Logger { get; }

    /// <inheritdoc/>
    protected override async Task OnServiceStarting()
    {
        await Task.CompletedTask;
        this.Logger.LogInformation("Mq consumer starting: {Queue}", this.QueueName);
    }

    /// <inheritdoc/>
    protected override async Task OnServiceStarted()
    {
        await Task.CompletedTask;
        this.Logger.LogInformation("Mq consumer started: {Queue}", this.QueueName);
    }

    /// <inheritdoc/>
    protected override async Task OnServiceStopping()
    {
        await Task.CompletedTask;
        this.Logger.LogInformation("Mq consumer stopping: {Queue}", this.QueueName);
    }

    /// <inheritdoc/>
    protected override async Task OnServiceStopped()
    {
        await Task.CompletedTask;
        this.Logger.LogInformation("Mq consumer stopped: {Queue}", this.QueueName);
    }

    /// <inheritdoc/>
    protected override async Task OnConsuming(object messageId, string json, int attempt)
    {
        await Task.CompletedTask;
        this.Logger.LogInformation(
            "Mq message incoming: {Queue}#{MessageId} ({Attempt}x)",
            this.QueueName,
            messageId,
            attempt);

        var tags = new Dictionary<string, object?>()
        {
            ["queue"] = this.QueueName,
            ["messageId"] = messageId,
            ["json"] = json,
            ["attempt"] = attempt,
        };
        using var activity = this.Telemeter.StartTrace("mq-consume", tags: tags.ToArray());
    }

    /// <inheritdoc/>
    protected override async Task OnConsumeSuccess(object messageId, string json, int attempt)
    {
        await base.OnConsumeSuccess(messageId, json, attempt);
        this.Logger.LogInformation(
            "Mq message success: {Queue}#{MessageId} ({Attempt}x)",
            this.QueueName,
            messageId,
            attempt);

        var tags = new Dictionary<string, object?>()
        {
            ["queue"] = this.QueueName,
        };
        this.Telemeter.CaptureMetric(MetricType.Counter, 1, "mq-consume-success", tags: tags.ToArray());
    }

    /// <inheritdoc/>
    protected override async Task OnConsumeFailure(object messageId, string json, int attempt, bool retry)
    {
        await base.OnConsumeFailure(messageId, json, attempt, retry);
        this.Logger.LogError(
            "Mq message failure ({FailMode}): {Queue}#{MessageId} ({Attempt}x)",
            retry ? "temporary" : "permanent",
            this.QueueName,
            messageId,
            attempt);

        var tags = new Dictionary<string, object?>()
        {
            ["outcome"] = retry ? "Retry" : "Abort",
            ["queue"] = this.QueueName,
        };
        this.Telemeter.CaptureMetric(MetricType.Counter, 1, "mq-consume-failure", tags: tags.ToArray());
    }
}