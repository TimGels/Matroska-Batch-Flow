using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models;
using MatroskaBatchFlow.Core.Models.AppSettings;

namespace MatroskaBatchFlow.Core.Services.FileValidation;

/// <summary>
/// Represents a validation rule that checks if files are in a supported file format.
/// </summary>
public class FileFormatValidationRule : IFileValidationRule
{
    /// <summary>
    /// Validates a collection of files to ensure they are in a supported file format.
    /// </summary>
    /// <remarks>An unsupported input blocks batch edits even if every configurable track check is off.</remarks>
    /// <param name="files">The collection of files to validate.</param>
    /// <param name="settings">Part of the rule contract; does not affect format eligibility or its error severity.</param>
    /// <returns>An enumerable collection of <see cref="FileValidationResult"/> indicating the validation results.</returns>
    public IEnumerable<FileValidationResult> Validate(IEnumerable<ScannedFileInfo> files, BatchValidationSettings settings)
    {
        foreach (var file in files)
        {
            if (file.UnsupportedFormatReason is { } reason)
            {
                yield return new FileValidationResult(ValidationSeverity.Error, file.Path, reason.Diagnostic)
                {
                    CheckKind = ValidationCheckKind.FileFormat,
                    FormatIssue = reason
                };
            }
        }
    }
}
