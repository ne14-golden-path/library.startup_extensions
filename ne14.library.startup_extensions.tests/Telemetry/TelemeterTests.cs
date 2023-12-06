// <copyright file="TelemeterTests.cs" company="ne1410s">
// Copyright (c) ne1410s. All rights reserved.
// </copyright>

namespace ne14.library.startup_extensions.tests.Telemetry;

using ne14.library.startup_extensions.Telemetry;

/// <summary>
/// Tests for the <see cref="Telemeter"/> class.
/// </summary>
public class TelemeterTests
{
    [Fact]
    public async Task MyMethod_WhenCalled_DoesExpected()
    {
        await Task.CompletedTask;
        1.Should().Be(1);
    }
}
