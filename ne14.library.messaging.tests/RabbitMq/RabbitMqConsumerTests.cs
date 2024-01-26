// <copyright file="RabbitMqConsumerTests.cs" company="ne1410s">
// Copyright (c) ne1410s. All rights reserved.
// </copyright>

namespace ne14.library.messaging.tests.RabbitMq;

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
    public async Task OnConsumerReceipt_WithArgs_CallsMessageReceived()
    {
        // Arrange
        var sut = GetSut<BasicConsumer>(out _);
        var received = 0;
        sut.MessageReceived += (_, _) => received++;

        // Act
        await sut.TestConsumerReceipt(null!, GetArgs());

        // Assert
        received.Should().Be(1);
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
        await act.Should().ThrowAsync<TransientFailureException>();
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
        await act.Should().ThrowAsync<PermanentFailureException>();
    }

    private static BasicDeliverEventArgs GetArgs()
    {
        var mockProps = new Mock<IBasicProperties>();
        return new()
        {
            BasicProperties = mockProps.Object,
            Body = new byte[] { 1, 2, 3 },
        };
    }

    private static MqConsumerEventArgs GetMqArgs(Guid? messageId = null)
    {
        return new()
        {
           AttemptNumber = 1,
           BornOn = 1,
           DeliveryId = 1,
           Message = "hi",
           MessageGuid = messageId ?? Guid.NewGuid(),
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
