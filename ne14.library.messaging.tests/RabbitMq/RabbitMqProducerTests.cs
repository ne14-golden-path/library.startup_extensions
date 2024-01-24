// <copyright file="RabbitMqProducerTests.cs" company="ne1410s">
// Copyright (c) ne1410s. All rights reserved.
// </copyright>

namespace ne14.library.messaging.tests.RabbitMq;

using FluentAssertions;
using ne14.library.messaging.Abstractions.Producer;
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

    [Fact]
    public void Ctor_NullReturningFactory_ThrowsException()
    {
        // Arrange
        var mockFactory = new Mock<IConnectionFactory>();

        // Act
        var act = () => new BasicProducer(mockFactory.Object);

        act.Should().Throw<ArgumentNullException>()
            .WithMessage("Value cannot be null. (Parameter 'connectionFactory')");
    }

    [Fact]
    public void Dispose_WhenCalled_ClosesChannelAndConnection()
    {
        // Arrange
        var sut = GetSut<BasicProducer>(out var mocks);

        // Act
        sut.Dispose();

        // Assert
        mocks.MockChannel.Verify(m => m.Close());
        mocks.MockConnection.Verify(m => m.Close());
    }

    private static T GetSut<T>(out BagOfMocks mocks)
       where T : MqProducerBase
    {
        mocks = new(
            new Mock<IModel>(),
            new Mock<IConnection>());

        var mockProps = new Mock<IBasicProperties>();
        mocks.MockChannel
            .Setup(m => m.CreateBasicProperties())
            .Returns(mockProps.Object);

        mocks.MockConnection
            .Setup(m => m.CreateModel())
            .Returns(mocks.MockChannel.Object);

        var mockConnectionFactory = new Mock<IConnectionFactory>();
        mockConnectionFactory
            .Setup(m => m.CreateConnection())
            .Returns(mocks.MockConnection.Object);

        return (T)Activator.CreateInstance(
            typeof(T),
            mockConnectionFactory.Object)!;
    }

    private sealed record BagOfMocks(
        Mock<IModel> MockChannel,
        Mock<IConnection> MockConnection);
}
