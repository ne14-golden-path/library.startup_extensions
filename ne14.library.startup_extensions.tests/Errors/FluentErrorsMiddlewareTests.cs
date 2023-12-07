// <copyright file="FluentErrorsMiddlewareTests.cs" company="ne1410s">
// Copyright (c) ne1410s. All rights reserved.
// </copyright>

namespace ne14.library.startup_extensions.tests.Errors;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using ne14.library.fluent_errors.Errors;
using ne14.library.startup_extensions.Errors;

/// <summary>
/// Tests for the <see cref="FluentErrorsMiddleware"/> class.
/// </summary>
public class FluentErrorsMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_NullContext_ThrowsException()
    {
        // Arrange
        var sut = GetSut(out _);

        // Act
        var act = () => sut.InvokeAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ResourceMissingException>();
    }

    [Fact]
    public async Task InvokeAsync_DefaultContext_DoesNotThrow()
    {
        // Arrange
        var sut = GetSut(out _);

        // Act
        var act = () => sut.InvokeAsync(new DefaultHttpContext());

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task InvokeAsync_RequestError_SetsStatus()
    {
        // Arrange
        var expected = new ArithmeticException("oops");
        var sut = GetSut(out var mockLogger, _ => throw expected);
        var context = new DefaultHttpContext();

        // Act
        await sut.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(500);
    }

    private static FluentErrorsMiddleware GetSut(
        out Mock<ILogger<FluentErrorsMiddleware>> mockLogger,
        Action<HttpContext>? requester = null)
    {
        mockLogger = new Mock<ILogger<FluentErrorsMiddleware>>();
        async Task Next(HttpContext ctx)
        {
            await Task.CompletedTask;
            requester?.Invoke(ctx);
        }

        return new(Next, mockLogger.Object);
    }
}
