namespace MatroskaBatchFlow.Core.Services.FileValidation
{
    public class FileValidator(IEnumerable<IFileValidationRule> rules) : IFileValidator
    {
        private readonly List<IFileValidationRule> _rules = [.. rules];

        /// <summary>
        /// Validates a collection of scanned files against a set of predefined rules.
        /// </summary>
        /// <remarks>This method iterates through all validation rules and applies them to the provided
        /// files. Each rule may produce one or more validation results. The results are returned as a lazily
        /// evaluated sequence.</remarks>
        /// <param name="files">A collection of <see cref="ScannedFileInfo"/> objects representing the files to validate. Cannot be null.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> of <see cref="FileValidationResult"/> objects, where each result represents
        /// the outcome of applying the validation rules to the files.</returns>
        public IEnumerable<FileValidationResult> Validate(IEnumerable<ScannedFileInfo> files)
        {
            foreach (var rule in _rules)
            {
                foreach (var result in rule.Validate(files))
                {
                    yield return result;
                }
            }
        }
    }
}
