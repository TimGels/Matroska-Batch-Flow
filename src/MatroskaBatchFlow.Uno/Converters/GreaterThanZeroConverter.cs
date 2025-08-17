using Microsoft.UI.Xaml.Data;

namespace MatroskaBatchFlow.Uno.Converters;

public partial class GreaterThanZeroConverter : IValueConverter
{
    /// <summary>
    /// Converts an integer value to a boolean indicating whether the value is greater than zero.
    /// </summary>
    /// <param name="value">The value to convert. Must be an <see cref="int"/> or convertible to an <see cref="int"/>.</param>
    /// <param name="targetType">The type to convert to. This parameter is not used in the conversion.</param>
    /// <param name="parameter">An optional parameter for the conversion. This parameter is not used in the conversion.</param>
    /// <param name="language">The culture or language information. This parameter is not used in the conversion.</param>
    /// <returns><see langword="true"/> if <paramref name="value"/> is an <see cref="int"/> and greater than zero; otherwise,
    /// <see langword="false"/>.</returns>
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is int count)
            return count > 0;

        return false;
    }

    /// <summary>
    /// Converts a value back to its source type.
    /// </summary>
    /// <param name="value">The value produced by the binding target.</param>
    /// <param name="targetType">The type to which the data must be converted.</param>
    /// <param name="parameter">An optional parameter to be used in the conversion logic.</param>
    /// <param name="language">The culture information for the conversion.</param>
    /// <returns>The converted value back to the source type.</returns>
    /// <exception cref="NotImplementedException">This method is not implemented.</exception>
    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}
