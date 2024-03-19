// <copyright file="ConsumerHostingService.cs" company="ne1410s">
// Copyright (c) ne1410s. All rights reserved.
// </copyright>

namespace ne14.library.messaging.Abstractions.Consumer;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

/// <summary>
/// Hosted singleton that allows for scoped consumers.
/// </summary>
/// <typeparam name="TConsumer">The consumer type.</typeparam>
public sealed class ConsumerHostingService<TConsumer> : IHostedService, IDisposable
    where TConsumer : MqConsumerBase
{
    private readonly IServiceScope originalScope;
    private readonly TConsumer consumer;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsumerHostingService{TConsumer}"/> class.
    /// </summary>
    /// <param name="services">The services.</param>
    public ConsumerHostingService(IServiceProvider services)
    {
        this.originalScope = services.CreateScope();
        this.consumer = this.originalScope.ServiceProvider.GetRequiredService<TConsumer>();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        this.originalScope.Dispose();
    }

    /// <inheritdoc/>
    public async Task StartAsync(CancellationToken cancellationToken)
        => await this.consumer.StartAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task StopAsync(CancellationToken cancellationToken)
        => await this.consumer.StopAsync(cancellationToken);
}
