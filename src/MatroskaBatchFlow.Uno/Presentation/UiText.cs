using System.Globalization;
using Microsoft.Windows.ApplicationModel.Resources;

namespace MatroskaBatchFlow.Uno.Presentation;

/// <summary>
/// Looks up UI text in the current language from the <c>Strings/&lt;language&gt;/Resources.resw</c> files.
/// </summary>
/// <remarks>
/// Every resource string is a complete phrase. For a counted phrase, the count selects one of two complete strings,
/// such as <c>ChecksFilesSingle</c> or <c>ChecksFilesMultiple</c>, instead of adding a plural ending to a translated word.
/// The choice follows the English rule: only a count of 1 uses the singular string.
/// </remarks>
public static class UiText
{
    private static readonly ResourceLoader Resources = new();

    /// <summary>
    /// Gets a resource string in the current language.
    /// </summary>
    /// <param name="key">The resource name, for example <c>ChecksClean</c>.</param>
    /// <returns>The resource string.</returns>
    public static string Get(string key) => Resources.GetString(key);

    /// <summary>
    /// Gets a resource string and fills in its placeholders, formatting numbers for the current culture.
    /// </summary>
    /// <param name="key">The resource name of a string with placeholders such as <c>{0}</c>.</param>
    /// <param name="args">The values for the placeholders, in order.</param>
    /// <returns>The resource string with its placeholders filled in.</returns>
    /// <exception cref="FormatException">The resource string has an invalid placeholder or one without a matching value.</exception>
    public static string Format(string key, params object?[] args) => string.Format(CultureInfo.CurrentCulture, Get(key), args);

    /// <summary>
    /// Gets the name of the singular or plural variant of a counted resource string.
    /// </summary>
    /// <param name="key">The base name, for example <c>ChecksFiles</c>.</param>
    /// <param name="count">The count that the phrase describes.</param>
    /// <returns>
    /// The base name followed by <c>Single</c> when <paramref name="count"/> is 1; otherwise, followed by <c>Multiple</c>.
    /// For example, <c>ChecksFilesMultiple</c>.
    /// </returns>
    public static string CountedKey(string key, int count) => key + (count == 1 ? "Single" : "Multiple");

    /// <summary>
    /// Gets the singular or plural variant of a counted resource string with the count filled in.
    /// </summary>
    /// <param name="key">The base name, for example <c>ChecksFiles</c>.</param>
    /// <param name="count">The count, inserted at placeholder <c>{0}</c>.</param>
    /// <returns>The phrase, for example "1 file" or "3 files".</returns>
    /// <exception cref="FormatException">The resource string has an invalid placeholder or one without a matching value.</exception>
    public static string Counted(string key, int count) => Format(CountedKey(key, count), count);
}
