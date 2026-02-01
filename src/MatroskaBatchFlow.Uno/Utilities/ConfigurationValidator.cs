using System.Reflection;
using MatroskaBatchFlow.Core.Attributes;

namespace MatroskaBatchFlow.Uno.Utilities;

/// <summary>
/// Provides startup validation for all configuration options marked with <see cref="ValidatedOptionsAttribute"/>.
/// </summary>
/// <param name="logger">The logger for validation diagnostics.</param>
public partial class ConfigurationValidator(ILogger<ConfigurationValidator> logger)
{
    /// <summary>
    /// Validates all registered configuration options at startup by triggering their lazy validation.
    /// </summary>
    /// <param name="services">The service provider containing the registered options.</param>
    /// <param name="assemblies">The assemblies to scan for option types. If null, scans the Core assembly.</param>
    /// <returns>A list of validation error messages, or an empty list if all options are valid.</returns>
    /// <remarks>
    /// <para>
    /// This method uses reflection to discover all types decorated with <see cref="ValidatedOptionsAttribute"/>
    /// and accesses their <see cref="IOptions{TOptions}.Value"/>, which triggers the validation configured via
    /// <see cref="OptionsBuilderDataAnnotationsExtensions.ValidateDataAnnotations{TOptions}(OptionsBuilder{TOptions})"/>.
    /// </para>
    /// <para>
    /// To add validation for a new option type:
    /// <list type="number">
    ///   <item><description>Add DataAnnotations validation attributes to the option class.</description></item>
    ///   <item><description>Decorate the class with <see cref="ValidatedOptionsAttribute"/>.</description></item>
    ///   <item><description>Register with <c>.ValidateDataAnnotations()</c> in ConfigurationExtensions.</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="services"/> is null.</exception>
    public List<string> ValidateAllOptions(IServiceProvider services, IEnumerable<Assembly>? assemblies = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var validationErrors = new List<string>();
        var assembliesToScan = (assemblies ?? [typeof(ValidatedOptionsAttribute).Assembly]).ToList();

        LogValidatingOptions(assembliesToScan.Count);

        var optionTypes = DiscoverOptionTypes(assembliesToScan);

        LogDiscoveredOptionTypes(optionTypes.Count);

        foreach (var optionType in optionTypes)
        {
            ValidateOptionType(services, optionType, validationErrors);
        }

        LogValidationCompleted(validationErrors.Count);

        return validationErrors;
    }

    /// <summary>
    /// Discovers all types decorated with <see cref="ValidatedOptionsAttribute"/> in the specified assemblies.
    /// </summary>
    /// <param name="assemblies">The assemblies to scan for option types.</param>
    private static List<Type> DiscoverOptionTypes(List<Assembly> assemblies)
    {
        return assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t.GetCustomAttribute<ValidatedOptionsAttribute>() is not null)
            .ToList();
    }

    /// <summary>
    /// Validates a single option type by accessing its IOptions&lt;T&gt;.Value property via reflection.
    /// </summary>
    /// <param name="services">The service provider containing the registered options.</param>
    /// <param name="optionType">The option type to validate.</param>
    /// <param name="validationErrors">The list to which validation error messages will be added.</param>
    private void ValidateOptionType(IServiceProvider services, Type optionType, List<string> validationErrors)
    {
        LogValidatingOptionType(optionType.Name);

        try
        {
            TriggerOptionValidation(services, optionType);
        }
        catch (TargetInvocationException ex) when (ex.GetBaseException() is OptionsValidationException)
        {
            HandleValidationException(optionType, ex, validationErrors);
        }
        catch (Exception ex)
        {
            HandleGeneralException(optionType, ex, validationErrors);
        }
    }

    /// <summary>
    /// Triggers validation by accessing the <see cref="IOptions{T}.Value"/> property for the given option type.
    /// </summary>
    /// <remarks>
    /// This method has side effects. It triggers validation logic and will throw if validation fails.
    /// </remarks>
    /// <param name="services">The service provider containing the registered options.</param>
    /// <param name="optionType">The option type to validate.</param>
    private void TriggerOptionValidation(IServiceProvider services, Type optionType)
    {
        var genericOptionsType = typeof(IOptions<>).MakeGenericType(optionType);
        var optionsInstance = services.GetRequiredService(genericOptionsType);

        var valueProperty = genericOptionsType.GetProperty("Value");
        if (valueProperty is null)
        {
            LogValuePropertyNotFound(optionType.Name);
            return;
        }

        // Access Value to trigger validation (result intentionally discarded)
        _ = valueProperty.GetValue(optionsInstance);
    }

    /// <summary>
    /// Handles <see cref="OptionsValidationException"/> by extracting and logging individual error messages.
    /// </summary>
    /// <param name="optionType">The option type being validated.</param>
    /// <param name="ex">The caught <see cref="TargetInvocationException"/>.</param>
    /// <param name="validationErrors">The list to which validation error messages will be added.</param>
    private void HandleValidationException(Type optionType, TargetInvocationException ex, List<string> validationErrors)
    {
        var errorMessages = SplitValidationErrorMessage(ex.GetBaseException().Message);

        foreach (var errorMessage in errorMessages)
        {
            LogValidationFailed(optionType.Name, errorMessage);
            validationErrors.Add($"{optionType.Name}: {errorMessage}");
        }
    }

    /// <summary>
    /// Handles general exceptions during option validation.
    /// </summary>
    /// <param name="optionType">The option type being validated.</param>
    /// <param name="ex">The caught exception.</param>
    /// <param name="validationErrors">The list to which validation error messages will be added.</param>
    private void HandleGeneralException(Type optionType, Exception ex, List<string> validationErrors)
    {
        var errorMessage = ex.GetBaseException().Message;
        LogValidationFailed(optionType.Name, errorMessage);
        validationErrors.Add($"{optionType.Name}: {errorMessage}");
    }

    /// <summary>
    /// Splits concatenated validation error messages (separated by "; ") into individual messages.
    /// </summary>
    /// <param name="errorMessage">The concatenated error message string.</param>
    /// <returns>A list of individual error messages.</returns>
    private static List<string> SplitValidationErrorMessage(string errorMessage)
    {
        return errorMessage
            .Split("; ", StringSplitOptions.RemoveEmptyEntries)
            .Select(msg => msg.Trim())
            .ToList();
    }
}
