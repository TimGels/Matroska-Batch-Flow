using System.Collections;
using Microsoft.UI.Xaml.Data;

namespace MatroskaBatchFlow.Uno.Converters;

public partial class EmptyStateToTextConverter : IValueConverter
{
    /// <summary>
    /// Converts a value to a string based on whether it is considered "empty" or "non-empty".
    /// </summary>
    /// <remarks>
    /// A value is considered empty if:
    /// <list type="bullet">
    ///   <item><description>It is <see langword="null"/>.</description></item>
    ///   <item><description>It is a <see cref="string"/> that is <see cref="string.IsNullOrWhiteSpace"/>.</description></item>
    ///   <item><description>It is an <see cref="ICollection"/> with a <see cref="ICollection.Count"/> of 0.</description></item>
    ///   <item><description>It is an <see cref="IEnumerable"/> with no elements.</description></item>
    ///   <item><description>It is a numeric value (<see cref="int"/>, <see cref="long"/>, <see cref="double"/>, <see cref="float"/>) equal to 0.</description></item>
    /// </list>
    /// All other values are considered non-empty.
    /// </remarks>
    /// <param name="value">The value to evaluate for emptiness.</param>
    /// <param name="targetType">The target type of the conversion. Not used.</param>
    /// <param name="parameter">
    /// A string containing two text values separated by a pipe ('|') character.
    /// The first value is returned if <paramref name="value"/> is empty; the second if not.
    /// If only one value is provided, it is used as the "empty" text, and the "non-empty" text defaults to an empty string.
    /// </param>
    /// <param name="language">The culture or language information. Not used.</param>
    /// <returns>
    /// The first text from <paramref name="parameter"/> if <paramref name="value"/> is empty; otherwise, the second text.
    /// </returns>
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        // Determine if the value is considered "empty".
        bool isEmpty = value switch
        {
            null => true,
            string s => string.IsNullOrWhiteSpace(s),
            ICollection col => col.Count == 0,
            IEnumerable enumerable => !enumerable.GetEnumerator().MoveNext(),
            int i => i == 0,
            long l => l == 0,
            double d => d == 0,
            float f => f == 0,
            _ => false
        };

        string[] texts = (parameter as string)?.Split('|') ?? [];
        string emptyText = texts.Length > 0 ? texts[0] : string.Empty;
        string nonEmptyText = texts.Length > 1 ? texts[1] : string.Empty;

        return isEmpty ? emptyText : nonEmptyText;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}
