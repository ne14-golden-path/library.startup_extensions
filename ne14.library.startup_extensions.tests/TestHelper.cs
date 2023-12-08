// <copyright file="TestHelper.cs" company="ne1410s">
// Copyright (c) ne1410s. All rights reserved.
// </copyright>

namespace ne14.library.startup_extensions.tests;

using Microsoft.Extensions.Logging;

/// <summary>
/// Class containing code that is common for unit tests.
/// </summary>
internal static class TestHelper
{
    public static void VerifyLog<T>(
        this Mock<ILogger<T>> mockLogger,
        LogLevel logLevel = LogLevel.Information,
        Func<string?, bool>? msgCheck = null,
        Func<Exception, bool>? exCheck = null)
    {
        mockLogger.Verify(
            m => m.Log(
                logLevel,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, _) => msgCheck == null || msgCheck(o.ToString())),
                It.Is<Exception>(ex => exCheck == null || exCheck(ex)),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()));
    }
}
