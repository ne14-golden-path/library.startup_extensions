// <copyright file="TracedMqProducerTests.cs" company="ne1410s">
// Copyright (c) ne1410s. All rights reserved.
// </copyright>

namespace ne14.library.startup_extensions.tests.Mq;

using Microsoft.Extensions.Logging;
using ne14.library.rabbitmq.Producer;
using ne14.library.rabbitmq.Vendor;
using ne14.library.startup_extensions.Mq;
using ne14.library.startup_extensions.Telemetry;
using RabbitMQ.Client;

/// <summary>
/// Tests for the <see cref="TracedMqProducer{T}"/> class.
/// </summary>
public class TracedMqProducerTests
{
    [Fact]
    public void Produce_WhenCalled_WritesExpectedLogs()
    {
        // Arrange
        var sut = GetSut<BasicTracedProducer>(out var mocks);

        // Act
        sut.Produce(new("bar", true));

        // Assert
        mocks.MockLogger.VerifyLog(msgCheck: s => s == "Mq message sending: " + sut.ExchangeName);
        mocks.MockLogger.VerifyLog(msgCheck: s => s == "Mq message sent: " + sut.ExchangeName);
    }

    private static T GetSut<T>(out BagOfMocks<T> mocks)
        where T : ProducerBase
    {
        mocks = new(
            new Mock<IRabbitMqSession>(),
            new Mock<IModel>(),
            new Mock<ITelemeter>(),
            new Mock<ILogger<T>>());

        mocks.MockSession
            .Setup(m => m.Channel)
            .Returns(mocks.MockChannel.Object);

        return (T)Activator.CreateInstance(
            typeof(T),
            mocks.MockSession.Object,
            mocks.MockTelemeter.Object,
            mocks.MockLogger.Object)!;
    }

    private record BagOfMocks<T>(
        Mock<IRabbitMqSession> MockSession,
        Mock<IModel> MockChannel,
        Mock<ITelemeter> MockTelemeter,
        Mock<ILogger<T>> MockLogger);
}
