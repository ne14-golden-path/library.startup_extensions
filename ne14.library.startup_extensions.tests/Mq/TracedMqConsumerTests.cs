// <copyright file="TracedMqConsumerTests.cs" company="ne1410s">
// Copyright (c) ne1410s. All rights reserved.
// </copyright>

namespace ne14.library.startup_extensions.tests.Mq;

using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ne14.library.fluent_errors.Errors;
using ne14.library.messaging.Abstractions.Consumer;
using ne14.library.startup_extensions.Mq;
using ne14.library.startup_extensions.Telemetry;
using RabbitMQ.Client;

/// <summary>
/// Tests for the <see cref="MqTracingConsumer{T}"/> class.
/// </summary>
public class TracedMqConsumerTests
{
    [Fact]
    public async Task StartAsync_WhenCalled_WritesExpectedLogs()
    {
        // Arrange
        var sut = GetSut<BasicTracedConsumer>(out var mocks);

        // Act
        await sut.StartAsync(CancellationToken.None);

        // Assert
        mocks.MockLogger.VerifyLog(msgCheck: s => s == "Mq consumer starting: " + sut.QueueName);
        mocks.MockLogger.VerifyLog(msgCheck: s => s == "Mq consumer started: " + sut.QueueName);
    }

    [Fact]
    public async Task StopAsync_WhenCalled_WritesExpectedLogs()
    {
        // Arrange
        var sut = GetSut<BasicTracedConsumer>(out var mocks);

        // Act
        await sut.StopAsync(CancellationToken.None);

        // Assert
        mocks.MockLogger.VerifyLog(msgCheck: s => s == "Mq consumer stopping: " + sut.QueueName);
        mocks.MockLogger.VerifyLog(msgCheck: s => s == "Mq consumer stopped: " + sut.QueueName);
    }

    [Fact]
    public async Task ConsumeAsync_WhenCalled_TracesActivity()
    {
        // Arrange
        var sut = GetSut<BasicTracedConsumer>(out var mocks);
        var payload = new BasicPayload("bar", false);
        var json = ToJson(payload);
        const string expectedName = "mq-consume";
        var args = new MqConsumerEventArgs { DeliveryId = 3ul, AttemptNumber = 3, Message = json };
        var tags = new KeyValuePair<string, object?>[]
        {
            new("queue", sut.QueueName),
            new("deliveryId", args.DeliveryId),
            new("json", json),
            new("attempt", args.AttemptNumber),
        };

        // Act
        await sut.ConsumeAsync(payload, args);

        // Assert
        mocks.MockTelemeter.Verify(
            m => m.StartTrace(expectedName, ActivityKind.Internal, tags));
    }

    [Fact]
    public async Task ConsumeAsync_Success_CapturesMetric()
    {
        // Arrange
        var sut = GetSut<BasicTracedConsumer>(out var mocks);
        var payload = new BasicPayload("hi", null);
        const string expectedName = "mq-consume-success";
        var tag = new KeyValuePair<string, object?>("queue", sut.QueueName);

        // Act
        await sut.ConsumeAsync(payload, new() { DeliveryId = 1ul, Message = ToJson(payload) });

        // Assert
        mocks.MockTelemeter.Verify(
            m => m.CaptureMetric(MetricType.Counter, 1, expectedName, null, null, null, tag));
    }

    [Theory]
    [InlineData(true, "retry")]
    [InlineData(false, "abort")]
    public async Task ConsumeAsync_Failure_CapturesMetric(bool transient, string expectedOutcome)
    {
        // Arrange
        var sut = GetSut<BasicTracedConsumer>(out var mocks);
        var payload = new BasicPayload("hi", transient);
        const string expectedName = "mq-consume-failure";
        var tags = new KeyValuePair<string, object?>[]
        {
            new("outcome", expectedOutcome),
            new("queue", sut.QueueName),
        };

        // Act
        await sut.ConsumeAsync(payload, new() { DeliveryId = 1ul });

        // Assert
        mocks.MockTelemeter.Verify(
            m => m.CaptureMetric(MetricType.Counter, 1, expectedName, null, null, null, tags));
    }

    [Fact]
    public async Task ConsumeAsync_Failure_InvokesBaseMethod()
    {
        // Arrange
        var sut = GetSut<BasicTracedConsumer>(out var mocks);
        var payload = new BasicPayload("hi", true);

        // Act
        await sut.ConsumeAsync(payload, new() { DeliveryId = 1ul });

        // Assert
        mocks.MockChannel.Verify(m => m.BasicNack(1ul, false, true));
    }

    [Fact]
    public async Task ConsumeAsync_Success_InvokesBaseMethod()
    {
        // Arrange
        var sut = GetSut<BasicTracedConsumer>(out var mocks);
        var payload = new BasicPayload("hi", null);

        // Act
        await sut.ConsumeAsync(payload, new() { DeliveryId = 1ul });

        // Assert
        mocks.MockChannel.Verify(m => m.BasicAck(1ul, false));
    }

    [Fact]
    public async Task ConsumeAsync_ConsumeSuccess_WritesExpectedLogs()
    {
        // Arrange
        var sut = GetSut<BasicTracedConsumer>(out var mocks);
        var payload = new BasicPayload("x", null);
        var suffix = $"{sut.QueueName}#4 (2x)";

        // Act
        await sut.ConsumeAsync(payload, new() { DeliveryId = 4ul, AttemptNumber = 2 });

        // Assert
        mocks.MockLogger.VerifyLog(msgCheck: s => s == "Mq message incoming: " + suffix);
        mocks.MockLogger.VerifyLog(msgCheck: s => s == "Mq message success: " + suffix);
    }

    [Fact]
    public async Task ConsumeAsync_ConsumePermanentFailure_WritesExpectedLogs()
    {
        // Arrange
        var sut = GetSut<BasicTracedConsumer>(out var mocks);
        var payload = new BasicPayload("x", false);
        var suffix = $"{sut.QueueName}#8 (1x)";

        // Act
        await sut.ConsumeAsync(payload, new() { DeliveryId = 8ul, AttemptNumber = 1 });

        // Assert
        mocks.MockLogger.VerifyLog(msgCheck: s => s == "Mq message incoming: " + suffix);
        mocks.MockLogger.VerifyLog(LogLevel.Error, s => s == "Mq message failure (permanent): " + suffix);
    }

    [Fact]
    public async Task ConsumeAsync_ConsumeTransientFailure_WritesExpectedLogs()
    {
        // Arrange
        var sut = GetSut<BasicTracedConsumer>(out var mocks);
        var payload = new BasicPayload("x", true);
        var suffix = $"{sut.QueueName}#1 (9x)";

        // Act
        await sut.ConsumeAsync(payload, new() { DeliveryId = 1ul, AttemptNumber = 9 });

        // Assert
        mocks.MockLogger.VerifyLog(msgCheck: s => s == "Mq message incoming: " + suffix);
        mocks.MockLogger.VerifyLog(LogLevel.Error, s => s == "Mq message failure (transient): " + suffix);
    }

    [Fact]
    public async Task ConsumeAsync_NullContext_ThrowsException()
    {
        // Arrange
        var sut = GetSut<BasicTracedConsumer>(out var mocks);
        var payload = new BasicPayload("x", null);

        // Act
        var act = () => sut.ConsumeAsync(payload, null!);

        // Assert
        await act.Should().ThrowAsync<DataStateException>();
    }

    private static string ToJson(object obj)
        => JsonSerializer.Serialize(obj);

    private static T GetSut<T>(out BagOfMocks<T> mocks, string appName = "test")
        where T : MqConsumerBase
    {
        var configDicto = new Dictionary<string, string?>()
        {
            ["RabbitMq:ConsumerAppName"] = appName,
        };

        var memConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(configDicto)
            .Build();

        mocks = new(
            new Mock<IModel>(),
            new Mock<ITelemeter>(),
            new Mock<ILogger<T>>());

        var mockProps = new Mock<IBasicProperties>();
        mocks.MockChannel
            .Setup(m => m.CreateBasicProperties())
            .Returns(mockProps.Object);

        var mockConnection = new Mock<IConnection>();
        mockConnection
            .Setup(m => m.CreateModel())
            .Returns(mocks.MockChannel.Object);

        var mockConnectionFactory = new Mock<IConnectionFactory>();
        mockConnectionFactory
            .Setup(m => m.CreateConnection())
            .Returns(mockConnection.Object);

        return (T)Activator.CreateInstance(
            typeof(T),
            mockConnectionFactory.Object,
            mocks.MockTelemeter.Object,
            mocks.MockLogger.Object,
            memConfig)!;
    }

    private sealed record BagOfMocks<T>(
        Mock<IModel> MockChannel,
        Mock<ITelemeter> MockTelemeter,
        Mock<ILogger<T>> MockLogger);
}
