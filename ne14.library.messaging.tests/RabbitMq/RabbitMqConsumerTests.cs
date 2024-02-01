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
        var sut = GetSut<BasicConsumer>(out _);
        var count = 0;
        sut.MessageProcessed += (_, _) => count++;

        // Act
        await sut.TestConsumerReceipt(null!, GetArgs());

        // Assert
        count.Should().Be(1);
    }

    [Fact]
    public async Task OnConsumerReceipt_WithInvalidJson_CallsMessageFailed()
    {
        // Arrange
        var sut = GetSut<BasicConsumer>(out _);
        var count = 0;
        sut.MessageFailed += (_, _) => count++;

        // Act
        await sut.TestConsumerReceipt(null!, GetArgs("<not-json>"));

        // Assert
        count.Should().Be(1);
    }

    [Fact]
    public async Task OnConsumerReceipt_WithHeaders_CallsExpected()
    {
        // Arrange
        var epoch = new DateTimeOffset(2002, 2, 14, 8, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds();
        var headers = new Dictionary<string, object>
        {
            ["x-attempt"] = 42L,
            ["x-born"] = epoch,
            ["x-guid"] = Guid.NewGuid().ToByteArray(),
        };

        var sut = GetSut<BasicConsumer>(out var mocks);
        var argsReceived = (MqConsumerEventArgs)null!;
        sut.MessageReceived += (_, args) => argsReceived = args;

        // Act
        await sut.TestConsumerReceipt(null!, GetArgs(headers: headers));

        // Assert
        argsReceived.BornOn.Should().Be(epoch);
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

    private static BasicDeliverEventArgs GetArgs(
        string json = "{}",
        Dictionary<string, object>? headers = null)
    {
        var mockProps = new Mock<IBasicProperties>();
        mockProps
            .Setup(m => m.Headers)
            .Returns(headers ?? []);
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
            new Mock<IConnection>());

        var mockProps = new Mock<IBasicProperties>();
        mocks.MockChannel
            .Setup(m => m.CreateBasicProperties())
            .Returns(mockProps.Object);
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
        Mock<IConnection> MockConnection);
}
