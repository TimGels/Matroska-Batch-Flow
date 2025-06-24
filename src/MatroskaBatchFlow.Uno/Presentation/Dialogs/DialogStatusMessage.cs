namespace MatroskaBatchFlow.Uno.Presentation.Dialogs;

/// <summary>
/// Represents the status of a dialog, including its name and whether it is currently open.
/// </summary>
/// <param name="DialogName">The name of the dialog. Cannot be null or empty.</param>
/// <param name="IsOpen">A value indicating whether the dialog is currently open. <see langword="true"/> if the dialog is open; otherwise,
/// <see langword="false"/>.</param>
public sealed record DialogStatusMessage(string DialogName, bool IsOpen);
