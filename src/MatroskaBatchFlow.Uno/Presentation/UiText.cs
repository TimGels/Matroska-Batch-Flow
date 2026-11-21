using System.Globalization;
using Microsoft.Windows.ApplicationModel.Resources;

namespace MatroskaBatchFlow.Uno.Presentation;

/// <summary>Looks up complete UI phrases in the current language. Counts select a template, not a suffix on a translated noun.</summary>
public static class UiText
{
    private static readonly ResourceLoader Resources = new();

    public static string Get(string key) => Resources.GetString(key);
    public static string Format(string key, params object?[] args) => string.Format(CultureInfo.CurrentCulture, Get(key), args);
    public static string CountedKey(string key, int count) => key + (count == 1 ? "Single" : "Multiple");
    public static string Counted(string key, int count) => Format(CountedKey(key, count), count);
}
