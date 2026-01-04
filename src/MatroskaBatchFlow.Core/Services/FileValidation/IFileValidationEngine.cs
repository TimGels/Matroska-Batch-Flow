using MatroskaBatchFlow.Core.Models;
using MatroskaBatchFlow.Core.Models.AppSettings;

namespace MatroskaBatchFlow.Core.Services.FileValidation;

/// <summary>
/// Defines a contract to coordinate and execute one or more validation rules over a set of files.
/// </summary>
public interface IFileValidationEngine
{
    /// <summary>
    /// Validates a collection of scanned files by use of defined validation rules.
    /// </summary>
    /// <param name="files">A collection of <see cref="ScannedFileInfo"/> objects representing the files to be validated. Each file must contain
    /// the necessary metadata required for validation.</param>
    /// <param name="settings">Validation settings controlling severity levels per track type and property.</param>
    /// <returns>An <see cref="IEnumerable{T}"/> of <see cref="FileValidationResult"/> objects, where each result represents the
    /// outcome of validating a corresponding file in the input collection.</returns>
    public IEnumerable<FileValidationResult> Validate(IEnumerable<ScannedFileInfo> files, BatchValidationSettings settings);
}
