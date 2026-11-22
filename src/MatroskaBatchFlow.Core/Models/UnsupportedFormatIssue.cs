namespace MatroskaBatchFlow.Core.Models;

public enum UnsupportedFormatKind
{
    MissingExtension,
    WrongExtension,
    UnknownContainer,
    WrongContainer
}

/// <summary>
/// File eligibility evidence. The diagnostic is for logs; clients can localize the kind and its scanned detail.
/// </summary>
public sealed record UnsupportedFormatIssue(UnsupportedFormatKind Kind, string? Detail = null)
{
    public string Diagnostic => Kind switch
    {
        UnsupportedFormatKind.MissingExtension => "Missing .mkv extension.",
        UnsupportedFormatKind.WrongExtension => $"Unsupported extension: {Detail}. Expected .mkv.",
        UnsupportedFormatKind.UnknownContainer => "The scan could not identify the container format.",
        UnsupportedFormatKind.WrongContainer => $"Detected container: {Detail}. Expected Matroska.",
        _ => throw new ArgumentOutOfRangeException(nameof(Kind))
    };
}
