// <copyright file="TransientFailureException.cs" company="ne1410s">
// Copyright (c) ne1410s. All rights reserved.
// </copyright>

namespace ne14.library.messaging.Abstractions.Consumer;

using System;

/// <summary>
/// A transient error.
/// </summary>
public class TransientFailureException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TransientFailureException"/> class.
    /// </summary>
    public TransientFailureException()
        : this("transient failure")
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="TransientFailureException"/> class.
    /// </summary>
    /// <param name="message">The message.</param>
    public TransientFailureException(string message)
        : this(message, null)
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="TransientFailureException"/> class.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <param name="innerException">The underlying exception.</param>
    public TransientFailureException(string message, Exception? innerException)
        : base(message, innerException)
    { }
}
