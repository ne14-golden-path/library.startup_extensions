// <copyright file="RabbitMqConsumerTests.cs" company="ne1410s">
// Copyright (c) ne1410s. All rights reserved.
// </copyright>

namespace ne14.library.messaging.tests.RabbitMq;

using System.Text;
using FluentAssertions;
using ne14.library.messaging.Abstractions.Consumer;
using ne14.library.messaging.RabbitMq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

/// <summary>
/// Tests for the <see cref="RabbitMqConsumer{T}"/> class.
/// </summary>
public class RabbitMqConsumerTests
{
    [Fact]
    public void Ctor_NullFactory_ThrowsException()
    {
        // Arrange
        var factory = (IConnectionFactory)null!;

        // Act
        var act = () => new BasicConsumer(factory);

        act.Should().Throw<ArgumentNullException>()
            .WithMessage("Value cannot be null. (Parameter 'connectionFactory')");
    }

    [Fact]
    public void Ctor_NullReturningFactory_ThrowsException()
    {
        // Arrange
        var mockFactory = new Mock<IConnectionFactory>();

        // Act
        var act = () => new BasicConsumer(mockFactory.Object);

        act.Should().Throw<ArgumentNullException>()
            .WithMessage("Value cannot be null. (Parameter 'connectionFactory')");
    }

    [Fact]
    public void Dispose_WhenCalled_ClosesChannelAndConnection()
    {
        // Arrange
        var sut = GetSut<BasicConsumer>(out var mocks);

        // Act
        sut.Dispose();

        // Assert
        mocks.MockChannel.Verify(m => m.Close());
        mocks.MockConnection.Verify(m => m.Close());
    }

    [Fact]
    public async Task OnConsumerReceipt_NullArgs_ThrowsException()
    {
        // Arrange
        var sut = GetSut<BasicConsumer>(out _);

        // Act
        var act = () => sut.TestConsumerReceipt(null!, null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithMessage("Value cannot be null. (Parameter 'args')");
    }

    [Fact]
    public async Task OnConsumerReceipt_WithValidJson_CallsMessageProcessed()
    {
        // Arrange
        var sut = GetSut<BasicConsumer>(out var mocks);

        // Act
        await sut.TestConsumerReceipt(null!, GetArgs());

        // Assert
        mocks.MockChannel.Verify(m => m.BasicAck(It.IsAny<ulong>(), false));
    }

    [Fact]
    public async Task OnConsumerReceipt_WithInvalidJson_CallsMessageFailed()
    {
        // Arrange
        var sut = GetSut<BasicConsumer>(out var mocks);

        // Act
        await sut.TestConsumerReceipt(null!, GetArgs("<not-json>"));

        // Assert
        mocks.MockChannel.Verify(m => m.BasicNack(It.IsAny<ulong>(), false, false));
    }

    [Fact]
    public async Task OnConsumerReceipt_WithHeaders_CallsExpected()
    {
        // Arrange
        var epoch = new DateTimeOffset(2002, 2, 14, 8, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds();
        var messageGuid = Guid.NewGuid();
        var headers = new Dictionary<string, object>
        {
            ["x-attempt"] = 42L,
            ["x-born"] = epoch,
            ["x-guid"] = messageGuid.ToByteArray(),
        };

        var sut = GetSut<BasicConsumer>(out var mocks);
        var argsReceived = (MqConsumerEventArgs)null!;
        sut.MessageReceived += (_, args) => argsReceived = args;

        // Act
        await sut.TestConsumerReceipt(null!, GetArgs(headers: headers));

        // Assert
        argsReceived.BornOn.Should().Be(epoch);
        argsReceived.AttemptNumber.Should().Be(42L);
        argsReceived.MessageGuid.Should().Be(messageGuid);
    }

    [Fact]
    public async Task StartStopCycle_WhenCalled_HitsExpected()
    {
        // Arrange
        var sut = GetSut<BasicConsumer>(out _);
        var events = new List<int>();
        sut.Starting += (_, _) => events.Add(1);
        sut.Started += (_, _) => events.Add(2);
        sut.Stopping += (_, _) => events.Add(3);
        sut.Stopped += (_, _) => events.Add(4);
        var expected = new[] { 1, 2, 3, 4 };

        // Act
        await sut.StartAsync(CancellationToken.None);
        await sut.StopAsync(CancellationToken.None);

        // Assert
        events.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task StartAsync_CalledTwice_CallsBasicConsumeOnce()
    {
        // Arrange
        var sut = GetSut<BasicConsumer>(out var mocks);
        var token = CancellationToken.None;

        // Act
        await sut.StartAsync(token);
        await sut.StartAsync(token);

        // Assert
        mocks.MockChannel.Verify(
            m => m.BasicConsume(
                sut.QueueName, false, It.IsAny<string>(), false, false, null, It.IsAny<IBasicConsumer>()),
            Times.Once());
    }

    [Fact]
    public async Task StartAsync_WhenCalled_DeclaresMainQueue()
    {
        // Arrange
        var sut = GetSut<BasicConsumer>(out var mocks);
        var expectedHeaders = new Dictionary<string, object>
        {
            ["x-dead-letter-exchange"] = sut.ExchangeName,
            ["x-dead-letter-routing-key"] = "T2_DLQ",
        };

        // Act
        await sut.StartAsync(CancellationToken.None);

        // Assert
        mocks.MockChannel.Verify(m => m.ExchangeDeclare(sut.ExchangeName, ExchangeType.Direct, true, false, null));
        mocks.MockChannel.Verify(m => m.QueueDeclare(sut.QueueName, true, false, false, expectedHeaders));
        mocks.MockChannel.Verify(m => m.QueueBind(sut.QueueName, sut.ExchangeName, "DEFAULT", null));
    }

    [Fact]
    public async Task StartAsync_WhenCalled_DeclaresRetryQueue()
    {
        // Arrange
        var sut = GetSut<BasicConsumer>(out var mocks);
        var expectedQueue = sut.QueueName + "_T1_RETRY";
        var expectedHeaders = new Dictionary<string, object>
        {
            ["x-dead-letter-exchange"] = sut.ExchangeName,
            ["x-dead-letter-routing-key"] = "DEFAULT",
        };

        // Act
        await sut.StartAsync(CancellationToken.None);

        // Assert
        mocks.MockChannel.Verify(m => m.QueueDeclare(expectedQueue, true, false, false, expectedHeaders));
        mocks.MockChannel.Verify(m => m.QueueBind(expectedQueue, sut.ExchangeName, "T1_RETRY", null));
    }

    [Fact]
    public async Task StartAsync_WhenCalled_DeclaresDeadLetterQueue()
    {
        // Arrange
        var sut = GetSut<BasicConsumer>(out var mocks);
        var expectedQueue = sut.QueueName + "_T2_DLQ";

        // Act
        await sut.StartAsync(CancellationToken.None);

        // Assert
        mocks.MockChannel.Verify(m => m.QueueDeclare(expectedQueue, true, false, false, null));
        mocks.MockChannel.Verify(m => m.QueueBind(expectedQueue, sut.ExchangeName, "T2_DLQ", null));
    }

    [Fact]
    public async Task StopAsync_CalledTwice_CallsBasicCancelOnce()
    {
        // Arrange
        var sut = GetSut<BasicConsumer>(out var mocks);
        var token = CancellationToken.None;

        // Act
        await sut.StartAsync(token);
        await sut.StopAsync(token);
        await sut.StopAsync(token);

        // Assert
        mocks.MockChannel.Verify(
            m => m.BasicCancel(It.IsAny<string>()),
            Times.Once());
    }

    [Fact]
    public async Task ConsumeAsync_WhenPassing_DoesNotThrow()
    {
        // Arrange
        var sut = GetSut<BasicConsumer>(out var mocks);
        var payload = new BasicPayload(null);

        // Act
        var act = () => sut.ConsumeAsync(payload, GetMqArgs());

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ConsumeAsync_WhenTempFail_ThrowsExpected()
    {
        // Arrange
        var sut = GetSut<BasicConsumer>(out var mocks);
        var payload = new BasicPayload(false);

        // Act
        var act = () => sut.ConsumeAsync(payload, GetMqArgs());

        // Assert
        await act.Should().ThrowAsync<TransientFailureException>()
            .WithMessage("transient failure");
    }

    [Fact]
    public async Task ConsumeAsync_WhenTempFail_SetsExpectedProperties()
    {
        // Arrange
        var sut = GetSut<BasicConsumer>(out var mocks);
        var actualHeaders = (IDictionary<string, object>)null!;
        var actualExpiration = (string)null!;
        mocks.MockProperties
            .SetupSet(p => p.Headers = It.IsAny<IDictionary<string, object>>())
            .Callback<IDictionary<string, object>>(value => actualHeaders = value);
        mocks.MockProperties
            .SetupSet(p => p.Expiration = It.IsAny<string>())
            .Callback<string>(value => actualExpiration = value);
        var expectedKeys = new[] { "x-attempt", "x-born", "x-guid" };

        // Act
        await sut.TestConsumerReceipt(null!, GetArgs("{ \"PermaFail\": false }"));

        // Assert
        actualExpiration.Should().NotBeNullOrEmpty();
        actualHeaders.Keys.Should().Contain(expectedKeys);
        actualHeaders["x-attempt"].Should().Be(2);
    }

    [Fact]
    public async Task ConsumeAsync_WhenTempFail_CallsChannelMethods()
    {
        // Arrange
        var sut = GetSut<BasicConsumer>(out var mocks);
        const string json = "{ \"PermaFail\": false }";

        // Act
        await sut.TestConsumerReceipt(null!, GetArgs(json));

        // Assert
        mocks.MockChannel.Verify(m => m.BasicAck(It.IsAny<ulong>(), false));
        mocks.MockChannel.Verify(
            m => m.BasicPublish(
                sut.ExchangeName, "T1_RETRY", false, It.IsAny<IBasicProperties>(), It.IsAny<ReadOnlyMemory<byte>>()));
    }

    [Fact]
    public async Task ConsumeAsync_WhenPermaFail_ThrowsExpected()
    {
        // Arrange
        var sut = GetSut<BasicConsumer>(out var mocks);
        var payload = new BasicPayload(true);

        // Act
        var act = () => sut.ConsumeAsync(payload, GetMqArgs());

        // Assert
        await act.Should().ThrowAsync<PermanentFailureException>()
            .WithMessage("permanent failure");
    }

    [Fact]
    public async Task ConsumeInternal_UnimplementedEventsInvalidJson_CallsMessageReceived()
    {
        // Arrange
        var sut = new GenericConsumer();
        var count = 0;
        sut.MessageReceived += (_, _) => count++;

        // Act
        await sut.TestConsume(new BasicPayload(true), GetMqArgs());

        // Assert
        count.Should().Be(1);
    }

    private static BasicDeliverEventArgs GetArgs(
        string json = "{}",
        Dictionary<string, object>? headers = null)
    {
        var mockProps = new Mock<IBasicProperties>();
        mockProps
            .Setup(m => m.Headers)
            .Returns(headers!);
        return new()
        {
            BasicProperties = mockProps.Object,
            Body = Encoding.UTF8.GetBytes(json),
        };
    }

    private static MqConsumerEventArgs GetMqArgs()
    {
        return new()
        {
           AttemptNumber = 1,
           BornOn = 1,
           DeliveryId = 1,
           Message = "hi",
           MessageGuid = Guid.NewGuid(),
        };
    }

    private static T GetSut<T>(out BagOfMocks mocks)
       where T : MqConsumerBase
    {
        mocks = new(
            new Mock<IModel>(),
            new Mock<IConnection>(),
            new Mock<IBasicProperties>());

        mocks.MockChannel
            .Setup(m => m.CreateBasicProperties())
            .Returns(mocks.MockProperties.Object);
        mocks.MockChannel
            .Setup(m => m.BasicConsume(
                It.IsAny<string>(), false, string.Empty, false, false, null, It.IsAny<IBasicConsumer>()))
            .Returns("tag");

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
        Mock<IConnection> MockConnection,
        Mock<IBasicProperties> MockProperties);
}
