namespace MatroskaBatchFlow.Uno.Messages;

/// <summary>
/// Message sent to request display of an unhandled exception dialog.
/// </summary>
/// <param name="Title">The title to display in the dialog header.</param>
/// <param name="Summary">A user-friendly summary of what went wrong.</param>
/// <param name="Exception">The exception that occurred.</param>
/// <param name="Timestamp">When the exception occurred.</param>
public sealed record ExceptionDialogMessage(
    string Title,
    string Summary,
    Exception Exception,
    DateTimeOffset Timestamp);
