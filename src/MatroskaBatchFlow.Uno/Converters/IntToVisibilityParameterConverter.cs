using Microsoft.UI.Xaml.Data;

namespace MatroskaBatchFlow.Uno.Converters;

/// <summary>
/// Converts an integer to Visibility, with customizable minimum threshold and visibility values via ConverterParameter.
/// Parameter format: "threshold;visibleValue;collapsedValue"
/// Example: "1;Visible;Collapsed" (Visible if value >= 1, Collapsed otherwise)
/// </summary>
public partial class IntThresholdToVisibilityConverter : IValueConverter
{
    /// <summary>
    ///   Converts an integer value to a <see cref="Visibility"/> value,
    ///   using a minimum threshold and optional custom visibility values.
    /// </summary>
    /// <param name="value">
    ///   The value to convert. Should be an <see cref="int"/>; if not,
    ///   defaults to 0.
    /// </param>
    /// <param name="targetType">
    ///   The type of the binding target property. Not used.
    /// </param>
    /// <param name="parameter">
    ///   An optional <see cref="string"/> in the format
    ///   <c>"threshold;whenTrue;whenFalse"</c>:
    ///   <list type="bullet">
    ///     <item>
    ///       <description>
    ///         <b>threshold</b>: The minimum integer value for which
    ///         <paramref name="whenTrue"/> is returned (inclusive).
    ///       </description>
    ///     </item>
    ///     <item>
    ///       <description>
    ///         <b>whenTrue</b>: The <see cref="Visibility"/> value to
    ///         return when <paramref name="value"/> is greater than or
    ///         equal to <c>threshold</c> (default: <c>Visible</c>).
    ///       </description>
    ///     </item>
    ///     <item>
    ///       <description>
    ///         <b>whenFalse</b>: The <see cref="Visibility"/> value to
    ///         return when <paramref name="value"/> is less than
    ///         <c>threshold</c> (default: <c>Collapsed</c>).
    ///       </description>
    ///     </item>
    ///   </list>
    ///   Example: <c>"1;Collapsed;Visible"</c> returns <c>Collapsed</c> if
    ///   value is at least 1, otherwise <c>Visible</c>.
    /// </param>
    /// <param name="language">
    ///   The culture or language information. Not used.
    /// </param>
    /// <returns>
    ///   The corresponding <see cref="Visibility"/> value based on the input
    ///   and parameter.
    /// </returns>
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        int intValue = value is int i ? i : 0;

        // Default values.
        int threshold = 0;
        Visibility whenTrue = Visibility.Visible;
        Visibility whenFalse = Visibility.Collapsed;

        if (parameter is string paramStr)
        {
            // Split the parameter string by semicolon.
            // Example: "1;Visible;Collapsed"
            var parts = paramStr.Split(';');
            if (parts.Length > 0 && int.TryParse(parts[0], out var t))
                threshold = t;
            if (parts.Length > 1 && Enum.TryParse(parts[1], out Visibility vTrue))
                whenTrue = vTrue;
            if (parts.Length > 2 && Enum.TryParse(parts[2], out Visibility vFalse))
                whenFalse = vFalse;
        }

        return intValue >= threshold ? whenTrue : whenFalse;
    }

    /// <summary>
    /// Converts a value back to its source type. Not implemented in this converter.
    /// </summary>
    /// <param name="value">The value produced by the binding target.</param>
    /// <param name="targetType">The type to which the value should be converted.</param>
    /// <param name="parameter">An optional parameter to use during the conversion.</param>
    /// <param name="language">The culture information for the conversion.</param>
    /// <returns>The converted value, which should match the source type of the binding.</returns>
    /// <exception cref="NotImplementedException">This method is not implemented.</exception>
    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}
