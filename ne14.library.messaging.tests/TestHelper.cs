// <copyright file="TestHelper.cs" company="ne1410s">
// Copyright (c) ne1410s. All rights reserved.
// </copyright>

namespace ne14.library.messaging.tests;

using System.Reflection;

/// <summary>
/// Class containing code that is common for unit tests.
/// </summary>
internal static class TestHelper
{
    public const string TestExchangeName = "basic-thing";

    public static void FireEvent<T>(
        this T source,
        string eventName,
        EventArgs? args = null)
    {
        var multiDelegate = typeof(T)
            .GetField(eventName, BindingFlags.Instance | BindingFlags.NonPublic)?
            .GetValue(source) as MulticastDelegate;

        foreach (var dlg in multiDelegate!.GetInvocationList())
        {
            dlg.Method.Invoke(dlg.Target, [null, args ?? EventArgs.Empty]);
        }
    }
}
