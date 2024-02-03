// <copyright file="MqConsumerBase.cs" company="ne1410s">
// Copyright (c) ne1410s. All rights reserved.
// </copyright>

namespace ne14.library.messaging.Abstractions.Consumer;

using System;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Base implementation for consuming mq messages.
/// </summary>
public abstract class MqConsumerBase : IMqConsumer
{
    private static readonly Regex KebabCaseRegex = new("(?<!^)([A-Z][a-z]|(?<=[a-z])[A-Z0-9])");

    /// <summary>
    /// Fires when the service is starting.
    /// </summary>
    public event EventHandler? Starting;

    /// <summary>
    /// Fires when the service has started.
    /// </summary>
    public event EventHandler? Started;

    /// <summary>
    /// Fires when the service is stopping.
    /// </summary>
    public event EventHandler? Stopping;

    /// <summary>
    /// Fires when the service has stopped.
    /// </summary>
    public event EventHandler? Stopped;

    /// <inheritdoc/>
    public abstract string ExchangeName { get; }

    /// <inheritdoc/>
    public string? ConsumerAppName { get; protected set; }

    /// <inheritdoc/>
    public long? MaximumAttempts { get; protected set; }

    /// <inheritdoc/>
    public string QueueName => ToKebabCase($"{this.ConsumerAppName}-{this.ExchangeName}");

    /// <inheritdoc/>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        this.Starting?.Invoke(this, EventArgs.Empty);
        await this.StartInternal(cancellationToken);
        this.Started?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc/>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        this.Stopping?.Invoke(this, EventArgs.Empty);
        await this.StopInternal(cancellationToken);
        this.Stopped?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Internal implementation for handling service start.
    /// </summary>
    /// <param name="token">The cancellation token.</param>
    /// <returns>Async task.</returns>
    protected internal abstract Task StartInternal(CancellationToken token);

    /// <summary>
    /// Internal implementation for handling service stop.
    /// </summary>
    /// <param name="token">The cancellation token.</param>
    /// <returns>Async task.</returns>
    protected internal abstract Task StopInternal(CancellationToken token);

    private static string ToKebabCase(string str)
        => KebabCaseRegex.Replace(str, "-$1").Trim().ToLower();
}

/// <inheritdoc cref="IMqConsumer{T}"/>
public abstract class MqConsumerBase<T> : MqConsumerBase, IMqConsumer<T>
{
    private readonly JsonSerializerOptions jsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>
    /// Fires when a message is received.
    /// </summary>
    public event EventHandler<MqConsumerEventArgs>? MessageReceived;

    /// <summary>
    /// Fires when a message has been handled successfully.
    /// </summary>
    public event EventHandler<MqConsumerEventArgs>? MessageProcessed;

    /// <summary>
    /// Fires when a message handler has been handled unsuccessfully.
    /// </summary>
    public event EventHandler<MqFailedEventArgs>? MessageFailed;

    /// <inheritdoc/>
    public abstract Task ConsumeAsync(T message, MqConsumerEventArgs args);

    /// <summary>
    /// Consumes raw message.
    /// </summary>
    /// <param name="rawMessage">The message bytes.</param>
    /// <param name="args">The event arguments.</param>
    /// <returns>Async task.</returns>
    protected internal async Task ConsumeInternal(byte[] rawMessage, MqConsumerEventArgs args)
    {
        args = args ?? throw new ArgumentNullException(nameof(args));
        this.MessageReceived?.Invoke(this, args);
        T? message = default;
        try
        {
            message = this.Parse(rawMessage);
            await this.ConsumeAsync(message, args);
            this.MessageProcessed?.Invoke(this, args);
        }
        catch (Exception ex)
        {
            var failEventArgs = new MqFailedEventArgs
            {
                Error = ex,
                AttemptNumber = args.AttemptNumber,
                BornOn = args.BornOn,
                DeliveryId = args.DeliveryId,
                Message = args.Message,
            };
            failEventArgs.Retry = this.DoRetry(failEventArgs);
            this.MessageFailed?.Invoke(this, failEventArgs);
        }
    }

    /// <summary>
    /// Categorically determines whether to retry.
    /// </summary>
    /// <param name="args">The error context.</param>
    /// <returns>Whether the message should be retried.</returns>
    protected virtual bool DoRetry(MqFailedEventArgs args)
    {
        var attempt = args?.AttemptNumber ?? throw new ArgumentNullException(nameof(args));
        return args.Error is not PermanentFailureException
            && (this.MaximumAttempts == null || attempt < this.MaximumAttempts);
    }

    private T Parse(byte[] message)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(message, this.jsonOpts)!;
        }
        catch (Exception ex)
        {
            throw new PermanentFailureException("Failed to parse message.", ex);
        }
    }
}