// <copyright file="MqExtensions.cs" company="ne1410s">
// Copyright (c) ne1410s. All rights reserved.
// </copyright>

namespace ne14.library.startup_extensions.Extensions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ne14.library.messaging.Abstractions.Consumer;
using ne14.library.messaging.Abstractions.Producer;
using RabbitMQ.Client;

/// <summary>
/// Extensions relating to message queue.
/// </summary>
public static class MqExtensions
{
    /// <summary>
    /// Adds the enterprise mq feature.
    /// </summary>
    /// <param name="services">The services.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The original parameter, for chainable commands.</returns>
    public static IServiceCollection AddEnterpriseMq(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var mqSection = configuration.GetRequiredSection("RabbitMq");
        var factory = new ConnectionFactory
        {
            UserName = mqSection["Username"],
            Password = mqSection["Password"],
            HostName = mqSection["Hostname"],
            Port = mqSection.GetValue<int?>("Port") ?? AmqpTcpEndpoint.UseDefaultPort,
            DispatchConsumersAsync = true,
        };

        return services.AddSingleton<IConnectionFactory>(_ => factory);
    }

    /// <summary>
    /// Adds a mq consumer service.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <typeparam name="T">The consumer type.</typeparam>
    /// <returns>The original parameter, for chainable commands.</returns>
    public static IServiceCollection AddMqConsumer<T>(
        this IServiceCollection services)
        where T : MqConsumerBase
    {
        return services
            .AddScoped<T>()
            .AddHostedService<ConsumerHostingService<T>>();
    }

    /// <summary>
    /// Adds a mq producer.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <typeparam name="T">The producer type.</typeparam>
    /// <returns>The original parameter, for chainable commands.</returns>
    public static IServiceCollection AddMqProducer<T>(
        this IServiceCollection services)
        where T : class, IMqProducer
    {
        return services.AddScoped<T>();
    }
}
