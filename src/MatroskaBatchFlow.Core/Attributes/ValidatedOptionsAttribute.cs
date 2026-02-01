namespace MatroskaBatchFlow.Core.Attributes;

/// <summary>
/// Marks an options class for automatic validation at application startup.
/// </summary>
/// <remarks>
/// <para>
/// Apply this attribute to option classes (e.g., <c>LoggingOptions</c>, <c>LanguageOptions</c>)
/// that should be validated during application startup.
/// </para>
/// <para>
/// The validator uses reflection to discover all types with this attribute and validates them
/// by triggering their <see cref="Microsoft.Extensions.Options.IOptions{TOptions}.Value"/> accessor,
/// which invokes any configured DataAnnotations validation.
/// </para>
/// <para>
/// Example usage:
/// <code>
/// [ValidatedOptions]
/// public class MyOptions
/// {
///     [Required]
///     public string RequiredSetting { get; set; } = string.Empty;
/// }
/// </code>
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class ValidatedOptionsAttribute : Attribute
{
}
