using Microsoft.UI.Xaml.Data;

namespace MatroskaBatchFlow.Uno.Converters;

/// <summary>
/// Converts a nullable value to Visibility - returns Visible if not null, Collapsed if null.
/// </summary>
public sealed class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, string language)
    {
        return value is not null ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object? parameter, string language)
    {
        throw new NotSupportedException("NullToVisibilityConverter does not support ConvertBack.");
    }
}
