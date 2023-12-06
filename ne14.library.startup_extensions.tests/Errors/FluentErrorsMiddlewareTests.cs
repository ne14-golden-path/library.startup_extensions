// <copyright file="FluentErrorsMiddlewareTests.cs" company="ne1410s">
// Copyright (c) ne1410s. All rights reserved.
// </copyright>

namespace ne14.library.startup_extensions.tests.Errors;

public class FluentErrorsMiddlewareTests
{
    [Fact]
    public async Task MyMethod_WhenCalled_DoesExpected()
    {
        await Task.CompletedTask;
        1.Should().Be(1);
    }
}
