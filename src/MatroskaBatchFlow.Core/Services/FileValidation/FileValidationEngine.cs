using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models;
using MatroskaBatchFlow.Core.Models.AppSettings;
using Microsoft.Extensions.Logging;

namespace MatroskaBatchFlow.Core.Services.FileValidation;

public partial class FileValidationEngine(
    IEnumerable<IFileValidationRule> rules,
    ILogger<FileValidationEngine> logger) : IFileValidationEngine
{
    private readonly List<IFileValidationRule> _rules = [.. rules];
    private readonly ILogger<FileValidationEngine> _logger = logger;

    /// <summary>
    /// Validates a collection of scanned files against a set of predefined rules.
    /// </summary>
    /// <remarks>This method iterates through all validation rules and applies them to the provided
    /// files. Each rule may produce one or more validation results. The results are returned as a lazily
    /// evaluated sequence.</remarks>
    /// <param name="files">A collection of <see cref="ScannedFileInfo"/> objects representing the files to validate. Cannot be null.</param>
    /// <param name="settings">Validation settings controlling severity levels per track type and property.</param>
    /// <returns>An <see cref="IEnumerable{T}"/> of <see cref="FileValidationResult"/> objects, where each result represents
    /// the outcome of applying the validation rules to the files.</returns>
    public IEnumerable<FileValidationResult> Validate(IEnumerable<ScannedFileInfo> files, BatchValidationSettings settings)
    {
        var fileList = files as IList<ScannedFileInfo> ?? files.ToList();
        LogValidationStarted(fileList.Count, _rules.Count, settings.Mode.ToString());

        foreach (var rule in _rules)
        {
            int ruleResultCount = 0;

            foreach (var result in rule.Validate(fileList, settings))
            {
                ruleResultCount++;

                if (result.Severity == ValidationSeverity.Error)
                {
                    LogValidationError(result.ValidatedFilePath, result.Message);
                }
                else if (result.Severity == ValidationSeverity.Warning)
                {
                    LogValidationWarning(result.ValidatedFilePath, result.Message);
                }
                else if (result.Severity == ValidationSeverity.Info)
                {
                    LogValidationInfo(result.ValidatedFilePath, result.Message);
                }

                yield return result;
            }

            LogRuleCompleted(rule.GetType().Name, ruleResultCount, fileList.Count);
        }
    }
}
