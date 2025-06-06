namespace MatroskaBatchFlow.Core.Services.FileValidation
{
    /// <summary>
    /// Defines a rule for validating a collection of scanned files.
    /// </summary>
    /// <remarks>Implementations of this interface are responsible for performing specific validation checks
    /// on files.</remarks>
    public interface IFileValidationRule
    {
        /// <summary>
        /// Validates a collection of scanned files and returns the results of the validation.
        /// </summary>
        /// <remarks>This method performs validation checks on each file in the provided collection, such
        /// as  verifying file integrity, format compliance, and other domain-specific criteria.  The caller can iterate
        /// over the returned results to determine the validation status of each file.</remarks>
        /// <param name="files">A collection of <see cref="ScannedFileInfo"/> objects representing the files to validate.  Cannot be null,
        /// and each file must contain valid metadata.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> of <see cref="FileValidationResult"/> objects, where each result  represents
        /// the outcome of validating a corresponding file in the input collection.</returns>
        IEnumerable<FileValidationResult> Validate(IEnumerable<ScannedFileInfo> files);
    }
}