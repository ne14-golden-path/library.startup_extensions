// <copyright file="FluentErrorsMiddlewareTests.cs" company="ne1410s">
// Copyright (c) ne1410s. All rights reserved.
// </copyright>

namespace ne14.library.startup_extensions.tests.Errors;

using FluentErrors.Errors;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
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
        var sut = GetSut(out _, _ => throw new ArithmeticException("oops"));
        var context = new DefaultHttpContext();

        // Act
        await sut.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task InvokeAsync_RequestError_LogsError()
    {
        // Arrange
        var sut = GetSut(out var mockLogger, _ => throw new ArithmeticException("oops"));

        // Act
        await sut.InvokeAsync(new DefaultHttpContext());

        // Assert
        mockLogger.VerifyLog(
            LogLevel.Error,
            s => s == "Unhandled exception",
            x => x.Message == "oops");
    }

    [Fact]
    public async Task InvokeAsync_RequestError_WritesContent()
    {
        // Arrange
        var sut = GetSut(out _, _ => throw new ArithmeticException("oops"));
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        await sut.InvokeAsync(context);

        // Assert
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        var response = await reader.ReadToEndAsync();
        response.Should().Contain(nameof(ArithmeticException));
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
