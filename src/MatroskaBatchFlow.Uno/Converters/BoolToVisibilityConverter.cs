using Microsoft.UI.Xaml.Data;

namespace MatroskaBatchFlow.Uno.Converters;

/// <summary>
/// Converts a boolean value to Visibility - returns Visible if true, Collapsed if false.
/// </summary>
public sealed class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, string language)
    {
        return value is true ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object? parameter, string language)
    {
        throw new NotSupportedException("BoolToVisibilityConverter does not support ConvertBack.");
    }
}
