using Microsoft.UI.Xaml.Data;

namespace MatroskaBatchFlow.Uno.Converters;

public partial class BoolToIndexConverter : IValueConverter
{
    /// <summary>
    /// Converts a boolean value to an index.
    /// </summary>
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return (value is bool b && b) ? 1 : 0;
    }

    /// <summary>
    /// Converts back from an index to a boolean value.
    /// </summary>
    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return (value is int i && i == 1);
    }
}
