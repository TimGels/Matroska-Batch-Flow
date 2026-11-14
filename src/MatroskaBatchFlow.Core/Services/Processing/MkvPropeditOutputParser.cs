using System.Globalization;
using System.Text.RegularExpressions;

namespace MatroskaBatchFlow.Core.Services.Processing;

/// <summary>
/// Parses the status lines that mkvpropedit writes while it runs.
/// </summary>
internal static partial class MkvPropeditOutputParser
{
    /// <summary>
    /// Attempts to read a percentage from a progress line such as <c>Progress: 42%</c>.
    /// </summary>
    /// <remarks>
    /// Only the shape <c>label: N%</c> is matched, so translated labels are recognised as well.
    /// </remarks>
    /// <param name="line">The output line to inspect.</param>
    /// <param name="percent">The parsed percentage, clamped to 0-100, when the line is a progress line.</param>
    /// <returns><see langword="true"/> if <paramref name="line"/> is a progress line; otherwise, <see langword="false"/>.</returns>
    public static bool TryParseProgress(string? line, out int percent)
    {
        percent = 0;

        if (string.IsNullOrWhiteSpace(line))
            return false;

        var match = ProgressLineRegex().Match(line);
        if (!match.Success)
            return false;

        percent = Math.Clamp(int.Parse(match.Groups["percent"].Value, CultureInfo.InvariantCulture), 0, 100);
        return true;
    }

    /// <summary>
    /// Removes progress lines from captured output so stored output only contains meaningful messages.
    /// </summary>
    /// <param name="output">The captured output, with lines separated by new lines.</param>
    /// <returns>The output without progress lines.</returns>
    public static string RemoveProgressLines(string output)
    {
        if (string.IsNullOrEmpty(output))
            return output;

        var lines = output.Split('\n')
            .Select(line => line.TrimEnd('\r'))
            .Where(line => !TryParseProgress(line, out _));

        return string.Join(Environment.NewLine, lines);
    }

    /// <summary>
    /// Matches a complete mkvpropedit progress line and captures its percentage.
    /// </summary>
    [GeneratedRegex(@"^\s*[^0-9:\uFF1A]+[:\uFF1A]\s*(?<percent>[0-9]{1,3})\s*%\s*$", RegexOptions.CultureInvariant)]
    private static partial Regex ProgressLineRegex();
}
