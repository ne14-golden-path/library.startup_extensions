// <copyright file="DiscoveryExtensionsTests.cs" company="ne1410s">
// Copyright (c) ne1410s. All rights reserved.
// </copyright>

namespace ne14.library.startup_extensions.tests.Extensions;

/// <summary>
/// Tests for the <see cref="DiscoveryExtensions"/> class.
/// </summary>
public class DiscoveryExtensionsTests
{
    [Fact]
    public async Task MyMethod_WhenCalled_DoesExpected()
    {
        await Task.CompletedTask;
        1.Should().Be(1);
    }
}
