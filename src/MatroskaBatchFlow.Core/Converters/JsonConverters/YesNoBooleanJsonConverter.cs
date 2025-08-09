using System.Text.Json;
using System.Text.Json.Serialization;

namespace MatroskaBatchFlow.Core.Converters.JsonConverters;

/// <summary>
/// A System.Text.Json converter that maps a boolean to the strings "Yes" and "No".
/// </summary>
/// <remarks>
/// - Deserialization is case-insensitive: the string "Yes" yields true; any other value yields false.
/// <br />
/// - Serialization writes "Yes" when the value is true and "No" when the value is false.
/// </remarks>
internal class YesNoBooleanJsonConverter : JsonConverter<bool>
{
    /// <summary>
    /// Reads a JSON value and converts the string "Yes"/"No" to a boolean.
    /// </summary>
    /// <param name="reader">The UTF-8 JSON reader positioned at the value to read.</param>
    /// <param name="typeToConvert">The target type being converted to (bool).</param>
    /// <param name="options">The serializer options in use.</param>
    /// <returns>
    /// <see langword="true"/> if the JSON string equals "Yes" (case-insensitive); otherwise, <see langword="false"/>.
    /// </returns>
    public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => reader.GetString()?.Equals("Yes", StringComparison.OrdinalIgnoreCase) == true;

    /// <summary>
    /// Writes a boolean value as the string "Yes" or "No".
    /// </summary>
    /// <param name="writer">The UTF-8 JSON writer to write to.</param>
    /// <param name="value">The boolean value to convert.</param>
    /// <param name="options">The serializer options in use.</param>
    public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
        => writer.WriteStringValue(value ? "Yes" : "No");
}
