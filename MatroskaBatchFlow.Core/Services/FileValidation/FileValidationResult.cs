namespace MatroskaBatchFlow.Core.Services.FileValidation
{
    /// <summary>
    /// Represents the severity level of a file validation result.
    /// </summary>
    public enum FileValidationSeverity
    {
        /// <summary>
        /// Reserved for future use, currently not used in validation rules.
        /// </summary>
        Info,
        /// <summary>
        /// Represents a warning message, indicating a potential issue that does not 
        /// prevent processing but should be noted. 
        /// </summary>
        /// <remarks> Use this severity for issues that may affect the quality or consistency of 
        /// the media files, such as inconsistent languages across files.</remarks>
        Warning,

        /// <summary>
        /// Represents an error message, indicating a significant issue that prevents the file 
        /// from being processed safely or correctly.
        /// </summary>
        /// <remarks> Use this severity for critical issues that must be resolved before 
        /// processing can continue, such as mismatched track counts or missing metadata.</remarks>
        Error
    }

    /// <summary>
    /// Represents the result of a file validation operation, including the severity of the validation result and an
    /// associated message.
    /// </summary>
    /// <param name="Severity">The severity of the validation result.</param>
    /// <param name="Message">A message providing details about the validation result.</param>
    public record FileValidationResult(FileValidationSeverity Severity, string Message);
}
