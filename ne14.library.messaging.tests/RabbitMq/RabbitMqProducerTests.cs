// <copyright file="RabbitMqProducerTests.cs" company="ne1410s">
// Copyright (c) ne1410s. All rights reserved.
// </copyright>

namespace ne14.library.messaging.tests.RabbitMq;

using FluentAssertions;
using ne14.library.messaging.RabbitMq;
using RabbitMQ.Client;

/// <summary>
/// Tests for the <see cref="RabbitMqProducer{T}"/> class.
/// </summary>
public class RabbitMqProducerTests
{
    [Fact]
    public void Ctor_NullFactory_ThrowsException()
    {
        // Arrange
        var factory = (IConnectionFactory)null!;

        // Act
        var act = () => new BasicProducer(factory);

        act.Should().Throw<ArgumentNullException>()
            .WithMessage("Value cannot be null. (Parameter 'connectionFactory')");
    }
}
