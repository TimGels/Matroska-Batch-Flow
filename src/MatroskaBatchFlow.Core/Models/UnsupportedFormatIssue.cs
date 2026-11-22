namespace MatroskaBatchFlow.Core.Models;

/// <summary>
/// Identifies why a scanned file cannot be edited in a batch.
/// </summary>
public enum UnsupportedFormatKind
{
    /// <summary>
    /// The file name has no extension.
    /// </summary>
    MissingExtension,

    /// <summary>
    /// The file extension is not .mkv. <see cref="UnsupportedFormatIssue.Detail"/> holds the extension.
    /// </summary>
    WrongExtension,

    /// <summary>
    /// The scan could not identify the container format.
    /// </summary>
    UnknownContainer,

    /// <summary>
    /// The container is not Matroska. <see cref="UnsupportedFormatIssue.Detail"/> holds the detected container.
    /// </summary>
    WrongContainer
}

/// <summary>
/// File eligibility evidence. The diagnostic is for logs; clients can localize the kind and its scanned detail.
/// </summary>
/// <param name="Kind">Why the file is unsupported.</param>
/// <param name="Detail">The scanned extension or container for <see cref="UnsupportedFormatKind.WrongExtension"/> and <see cref="UnsupportedFormatKind.WrongContainer"/>; otherwise, null.</param>
public sealed record UnsupportedFormatIssue(UnsupportedFormatKind Kind, string? Detail = null)
{
    /// <summary>
    /// Gets an English description of the issue, used for logs and the validation result message.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><see cref="Kind"/> is not a defined value.</exception>
    public string Diagnostic => Kind switch
    {
        UnsupportedFormatKind.MissingExtension => "Missing .mkv extension.",
        UnsupportedFormatKind.WrongExtension => $"Unsupported extension: {Detail}. Expected .mkv.",
        UnsupportedFormatKind.UnknownContainer => "The scan could not identify the container format.",
        UnsupportedFormatKind.WrongContainer => $"Detected container: {Detail}. Expected Matroska.",
        _ => throw new ArgumentOutOfRangeException(nameof(Kind))
    };
}
