using Microsoft.UI.Xaml.Data;

namespace MatroskaBatchFlow.Uno.Converters;

/// <summary>
/// Provides a value converter that inverts a boolean value.
/// </summary>
/// <remarks>This converter is typically used in data binding scenarios where a boolean value needs to be
/// inverted. For example, it can be used to bind a property to a UI element where the logic requires the 
/// opposite value.</remarks>
public class InverseBoolConverter : IValueConverter
{
    /// <summary>
    /// Converts a boolean value to its negated value (i.e., true becomes false and vice versa).
    /// </summary>
    /// <param name="value">The value to convert. Must be of type <see langword="bool"/>.</param>
    /// <param name="targetType">The type to convert to. This parameter is not used in the conversion.</param>
    /// <param name="parameter">An optional parameter for the conversion. This parameter is not used in the conversion.</param>
    /// <param name="language">The culture-specific language information. This parameter is not used in the conversion.</param>
    /// <returns>The negated boolean value if <paramref name="value"/> is of type <see langword="bool"/>;  otherwise, returns the
    /// original <paramref name="value"/>.</returns>
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is not bool boolValue)
            return value;
        
        return !boolValue;
    }

    /// <summary>
    /// Converts a boolean back to its non negated value (i.e., the original boolean value).
    /// </summary>
    /// <param name="value">The value to be converted. Must be of type <see langword="bool"/>.</param>
    /// <param name="targetType">The type to convert to. This parameter is not used in this implementation.</param>
    /// <param name="parameter">An optional parameter for the conversion. This parameter is not used in this implementation.</param>
    /// <param name="language">The culture information for the conversion. This parameter is not used in this implementation.</param>
    /// <returns>The negated boolean value of the input <paramref name="value"/>.</returns>
    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is not bool boolValue)
            return value;
        
        return !boolValue;
    }
}
