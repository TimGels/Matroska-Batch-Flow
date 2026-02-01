namespace MatroskaBatchFlow.Uno.Utilities;

/// <summary>
/// LoggerMessage definitions for <see cref="ConfigurationValidator"/>.
/// </summary>
public partial class ConfigurationValidator
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "Validating configuration options from {AssemblyCount} assemblies")]
    private partial void LogValidatingOptions(int assemblyCount);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Discovered {OptionCount} option types to validate")]
    private partial void LogDiscoveredOptionTypes(int optionCount);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Validating option type: {OptionType}")]
    private partial void LogValidatingOptionType(string optionType);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Configuration validation failed for {OptionType}: {ErrorMessage}")]
    private partial void LogValidationFailed(string optionType, string errorMessage);

    [LoggerMessage(Level = LogLevel.Information, Message = "Configuration validation completed with {ErrorCount} errors")]
    private partial void LogValidationCompleted(int errorCount);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Option type {OptionType} missing IOptions<T>.Value property")]
    private partial void LogValuePropertyNotFound(string optionType);
}
