using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models;
using MatroskaBatchFlow.Core.Models.AppSettings;
using Microsoft.Extensions.Logging;

namespace MatroskaBatchFlow.Core.Services.FileValidation;

/// <summary>
/// Checks file eligibility before comparing tracks, so an unsupported input cannot become a reference for other files.
/// </summary>
/// <param name="formatRule">Mandatory format check applied to every input, regardless of validation mode.</param>
/// <param name="rules">Consistency rules for supported files only; this collection must not include the format rule.</param>
/// <param name="logger">Receives diagnostics as the results are enumerated.</param>
public partial class FileValidationEngine(
    FileFormatValidationRule formatRule,
    IEnumerable<IFileValidationRule> rules,
    ILogger<FileValidationEngine> logger) : IFileValidationEngine
{
    private readonly List<IFileValidationRule> _rules = [.. rules];
    private readonly ILogger<FileValidationEngine> _logger = logger;

    /// <summary>
    /// Validates a collection of scanned files against a set of predefined rules.
    /// </summary>
    /// <remarks>
    /// Enumeration yields format findings first. Subsequent rules receive only the remaining files, in batch order.
    /// </remarks>
    /// <param name="files">A collection of <see cref="ScannedFileInfo"/> objects representing the files to validate. Cannot be null.</param>
    /// <param name="settings">Validation settings controlling severity levels per track type and property.</param>
    /// <returns>An <see cref="IEnumerable{T}"/> of <see cref="FileValidationResult"/> objects, where each result represents
    /// the outcome of applying the validation rules to the files.</returns>
    public IEnumerable<FileValidationResult> Validate(IEnumerable<ScannedFileInfo> files, BatchValidationSettings settings)
    {
        var fileList = files as IReadOnlyList<ScannedFileInfo> ?? files.ToList();
        LogValidationStarted(fileList.Count, _rules.Count + 1, settings.Mode.ToString());

        var unsupported = new HashSet<string>(StringComparer.Ordinal);
        foreach (var result in RunRule(formatRule, fileList, settings))
        {
            unsupported.Add(result.ValidatedFilePath);
            yield return result;
        }

        var supported = unsupported.Count == 0 ? fileList : fileList.Where(f => !unsupported.Contains(f.Path)).ToArray();
        foreach (var rule in _rules)
            foreach (var result in RunRule(rule, supported, settings))
                yield return result;
    }

    private IEnumerable<FileValidationResult> RunRule(
        IFileValidationRule rule, IReadOnlyList<ScannedFileInfo> files, BatchValidationSettings settings)
    {
        int ruleResultCount = 0;
        foreach (var result in rule.Validate(files, settings))
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
        LogRuleCompleted(rule.GetType().Name, ruleResultCount, files.Count);
    }
}
