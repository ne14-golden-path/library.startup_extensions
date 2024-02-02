// <copyright file="MqConsumerBaseTests.cs" company="ne1410s">
// Copyright (c) ne1410s. All rights reserved.
// </copyright>

namespace ne14.library.messaging.tests.Abstractions;

using FluentAssertions;
using ne14.library.messaging.Abstractions.Consumer;

public class MqConsumerBaseTests
{
    [Fact]
    public async Task ConsumeInternal_UnimplementedEvents_CallsMessageReceived()
    {
        // Arrange
        var sut = new GenericConsumer();
        var count = 0;
        sut.MessageReceived += (_, _) => count++;

        // Act
        await sut.TestConsume(new(null), GetMqArgs());
        await sut.StartAsync(CancellationToken.None);
        await sut.StopAsync(CancellationToken.None);

        // Assert
        count.Should().Be(1);
        sut.ExchangeName.Should().Be(TestHelper.TestExchangeName);
    }

    [Theory]
    [InlineData(1L, 5L, true)]
    [InlineData(1L, null, true)]
    [InlineData(5L, 5L, false)]
    public async Task ConsumeInternal_VaryingTempFailAttempts_RetryAsExpected(
        long attempt, long? maxAttempts, bool expectRetry)
    {
        // Arrange
        var sut = new GenericConsumer(maxAttempts);
        var failArgs = (MqFailedEventArgs)null!;
        sut.MessageFailed += (_, args) => failArgs = args;

        // Act
        await sut.TestConsume(new(false), GetMqArgs(attempt));

        // Assert
        failArgs.Retry.Should().Be(expectRetry);
    }

    private static MqConsumerEventArgs GetMqArgs(long attempt = 1)
    {
        return new()
        {
            AttemptNumber = attempt,
            BornOn = 1,
            DeliveryId = 1,
            Message = "hi",
            MessageGuid = Guid.NewGuid(),
        };
    }
}
