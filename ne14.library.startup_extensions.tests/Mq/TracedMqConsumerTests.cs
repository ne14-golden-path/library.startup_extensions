// <copyright file="TracedMqConsumerTests.cs" company="ne1410s">
// Copyright (c) ne1410s. All rights reserved.
// </copyright>

namespace ne14.library.startup_extensions.tests.Mq;

using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ne14.library.rabbitmq.Consumer;
using ne14.library.rabbitmq.Vendor;
using ne14.library.startup_extensions.Mq;
using ne14.library.startup_extensions.Telemetry;
using RabbitMQ.Client;

/// <summary>
/// Tests for the <see cref="TracedMqConsumer{T}"/> class.
/// </summary>
public class TracedMqConsumerTests
{
    [Fact]
    public async Task StartAsync_WhenCalled_WritesExpectedLogs()
    {
        // Arrange
        var sut = GetSut<BasicTracedConsumer>(out var mocks);
        const string queue = "q-ne-14.library.startup_extensions-basic-thing";

        // Act
        await sut.StartAsync(CancellationToken.None);

        // Assert
        mocks.MockLogger.VerifyLog(msgCheck: s => s == "Mq consumer starting: " + queue);
        mocks.MockLogger.VerifyLog(msgCheck: s => s == "Mq consumer started: " + queue);
    }

    [Fact]
    public async Task StopAsync_WhenCalled_WritesExpectedLogs()
    {
        // Arrange
        var sut = GetSut<BasicTracedConsumer>(out var mocks);
        const string queue = "q-ne-14.library.startup_extensions-basic-thing";

        // Act
        await sut.StopAsync(CancellationToken.None);

        // Assert
        mocks.MockLogger.VerifyLog(msgCheck: s => s == "Mq consumer stopping: " + queue);
        mocks.MockLogger.VerifyLog(msgCheck: s => s == "Mq consumer stopped: " + queue);
    }

    [Fact]
    public async Task ConsumeAsync_ConsumeSuccess_WritesExpectedLogs()
    {
        // Arrange
        var sut = GetSut<BasicTracedConsumer>(out var mocks);
        var bytes = ToBytes(new BasicPayload("x", null));
        const string queue = "q-ne-14.library.startup_extensions-basic-thing";
        const string suffix = $"{queue}#4 (2x)";

        // Act
        await sut.ConsumeAsync(bytes, new() { MessageId = 4ul, AttemptNumber = 2 });

        // Assert
        mocks.MockLogger.VerifyLog(msgCheck: s => s == "Mq message incoming: " + suffix);
        mocks.MockLogger.VerifyLog(msgCheck: s => s == "Mq message success: " + suffix);
    }

    [Fact]
    public async Task ConsumeAsync_ConsumePermanentFailure_WritesExpectedLogs()
    {
        // Arrange
        var sut = GetSut<BasicTracedConsumer>(out var mocks);
        var bytes = ToBytes(new BasicPayload("x", false));
        const string queue = "q-ne-14.library.startup_extensions-basic-thing";
        const string suffix = $"{queue}#8 (1x)";

        // Act
        await sut.ConsumeAsync(bytes, new() { MessageId = 8ul, AttemptNumber = 1 });

        // Assert
        mocks.MockLogger.VerifyLog(msgCheck: s => s == "Mq message incoming: " + suffix);
        mocks.MockLogger.VerifyLog(LogLevel.Error, s => s == "Mq message failure (permanent): " + suffix);
    }

    [Fact]
    public async Task ConsumeAsync_ConsumeTransientFailure_WritesExpectedLogs()
    {
        // Arrange
        var sut = GetSut<BasicTracedConsumer>(out var mocks);
        var bytes = ToBytes(new BasicPayload("x", true));
        const string queue = "q-ne-14.library.startup_extensions-basic-thing";
        const string suffix = $"{queue}#1 (9x)";

        // Act
        await sut.ConsumeAsync(bytes, new() { MessageId = 1ul, AttemptNumber = 9 });

        // Assert
        mocks.MockLogger.VerifyLog(msgCheck: s => s == "Mq message incoming: " + suffix);
        mocks.MockLogger.VerifyLog(LogLevel.Error, s => s == "Mq message failure (transient): " + suffix);
    }

    private static byte[] ToBytes(object obj)
        => Encoding.UTF8.GetBytes(JsonSerializer.Serialize(obj));

    private static T GetSut<T>(out BagOfMocks<T> mocks)
        where T : ConsumerBase
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
