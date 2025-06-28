using Microsoft.UI.Xaml.Data;

namespace MatroskaBatchFlow.Uno.Converters;

/// <summary>
/// Provides methods to convert zero-based index values to one-based index values and vice versa.
/// </summary>
public class ZeroBasedToOneBasedConverter : IValueConverter
{
    /// <summary>
    /// Converts a zero-based index value to a one-based index value.
    /// </summary>
    /// <param name="value">The value to convert. Must be an <see cref="int"/> to be processed; otherwise, the original value is returned.</param>
    /// <param name="targetType">The desired type of the conversion result. If <see cref="string"/>, the result is returned as a string.</param>
    /// <param name="parameter">An optional parameter that is not used in this implementation.</param>
    /// <param name="language">The culture or language information, which is not used in this implementation.</param>
    /// <returns>The incremented value of <paramref name="value"/> as an <see cref="int"/> or a <see cref="string"/>,  depending on
    /// the <paramref name="targetType"/>. If <paramref name="value"/> is not an <see cref="int"/>,  the original value is
    /// returned.</returns>
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is not int position)
            return value;

        var result = position + 1;

        // If the target property expects a string, return a string.
        // This is useful in x:binding scenarios where the expected type is a string.
        if (targetType == typeof(string))
            return result.ToString();

        return result;
    }

    /// <summary>
    /// Converts a value back to its original form by adjusting the position index.
    /// </summary>
    /// <param name="value">The value to convert back. Expected to be an integer representing a position.</param>
    /// <param name="targetType">The type to which the value should be converted. If <see langword="typeof(string)"/>, 
    /// the result will be returned as a string.</param>
    /// <param name="parameter">An optional parameter for the conversion. This parameter is not used in this implementation.</param>
    /// <param name="language">The culture-specific language information. This parameter is not used in this implementation.</param>
    /// <returns>The adjusted position as an integer or a string, depending on the <paramref name="targetType"/>. If <paramref
    /// name="value"/> is not an integer, the original <paramref name="value"/> is returned.</returns>
    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is not int humanPosition)
            return value;

        var result = humanPosition - 1;

        // If the target property expects a string, return a string.
        // This is useful in x:binding scenarios where the expected type is a string.
        if (targetType == typeof(string))
            return result.ToString();

        return result;
    }
}
