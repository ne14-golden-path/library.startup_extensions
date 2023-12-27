﻿// <copyright file="MqExtensions.cs" company="ne1410s">
// Copyright (c) ne1410s. All rights reserved.
// </copyright>

namespace ne14.library.startup_extensions.Extensions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ne14.library.rabbitmq;

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

        var user = mqSection.GetValue<string>("Username");
        var pass = mqSection.GetValue<string>("Password");
        var host = mqSection.GetValue<string>("Hostname");

        return services.AddSingleton(_ => new RabbitMqSession(user, pass, host));
    }

    /// <summary>
    /// Adds a mq consumer service.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <typeparam name="T">The consumer type.</typeparam>
    /// <returns>The original parameter, for chainable commands.</returns>
    public static IServiceCollection AddMqConsumer<T>(
        this IServiceCollection services)
        where T : ConsumerBase
    {
        return services.AddHostedService<T>();
    }
}
