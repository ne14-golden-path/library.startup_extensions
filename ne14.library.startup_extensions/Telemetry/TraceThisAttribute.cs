// <copyright file="TraceThisAttribute.cs" company="ne1410s">
// Copyright (c) ne1410s. All rights reserved.
// </copyright>

namespace ne14.library.startup_extensions.Telemetry;

using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using MethodBoundaryAspect.Fody.Attributes;

/// <summary>
/// Attribute that automatically captures basic trace data.
/// </summary>
[AttributeUsage(AttributeTargets.All)]
public sealed class TraceThisAttribute : OnMethodBoundaryAspect, IDisposable
{
    private readonly ITelemeter telemeter;
    private Activity? activity;

    /// <summary>
    /// Initializes a new instance of the <see cref="TraceThisAttribute"/> class.
    /// </summary>
    public TraceThisAttribute()
    {
        this.telemeter = new Telemeter();
    }

    /// <inheritdoc/>
    public override void OnEntry(MethodExecutionArgs arg)
    {
        var method = arg?.Method ?? throw new ArgumentNullException(nameof(arg));
        var activityName = GetActivityName(method);
        this.activity = this.telemeter.StartTrace(activityName);

        Debug.WriteLine($"Activity on {this.telemeter.Name}; {activityName}");
    }

    /// <inheritdoc/>
    public override void OnExit(MethodExecutionArgs arg)
    {
        if (arg?.ReturnValue is Task task)
        {
            task.ContinueWith(_ => this.Dispose(), TaskScheduler.Default);
        }
        else
        {
            this.Dispose();
        }
    }

    /// <inheritdoc/>
    public override void OnException(MethodExecutionArgs arg)
    {
        var ex = arg?.Exception ?? throw new ArgumentNullException(nameof(arg));
        var tags = new ActivityTagsCollection
        {
            ["type"] = ex.GetType().Name,
            ["message"] = ex.Message,
        };

        if (ex.InnerException != null)
        {
            tags.Add("innerType", ex.InnerException.GetType().Name);
            tags.Add("innerMessage", ex.InnerException.Message);
        }

        this.activity?.AddEvent(new("exception", tags: tags));
        this.Dispose();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        this.activity?.Stop();
        this.activity?.Dispose();
    }

    private static string GetActivityName(MethodBase method)
    {
        var declaringAssembly = Assembly.GetAssembly(method.DeclaringType!);
        var prefix = declaringAssembly?.GetName()?.Name;
        return $"[{prefix}] {method.DeclaringType?.Name}::{method.Name}()";
    }
}