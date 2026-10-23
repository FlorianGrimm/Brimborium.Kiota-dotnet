using System.Text;

namespace Brimborium.UriTemplate;

/// <summary>
/// Character classification and encoding helpers shared by URI-template and
/// OData value formatting.
/// </summary>
internal static class UriCharacterUtility {
    /// <summary>
    /// Appends the contents of an OData string literal (without the enclosing
    /// quotes), percent-encoding URI-sensitive characters and doubling apostrophes.
    /// </summary>
    /// <param name="stringValue">The string contents to append.</param>
    /// <param name="output">The target.</param>
    internal static void AppendODataStringContent(string stringValue, StringBuilder output) {
        _ = output.EnsureCapacity(output.Length + stringValue.Length * 2); // hint to SB

        for (int pos = 0; pos < stringValue.Length; pos++) {
            char character = stringValue[pos];

            if (IsSurrogate(character)) {
                if (AppendSurrogate(stringValue, pos, output)) {
                    pos++; // skip the low surrogate
                }
            } else if (IsUcschar(character) || IsIprivate(character)) {
                _ = output.Append(Uri.EscapeDataString(character.ToString()));
            } else if (character == ' ') {
                _ = output.Append("%20");
            } else if (character == '%') {
                _ = output.Append("%25");
            } else if (character == '\'') {
                _ = output.Append("\'\'");
            } else if (char.IsAsciiLetter(character) || char.IsAsciiDigit(character)) {
                _ = output.Append(character);
            } else {
                _ = output.Append(Uri.EscapeDataString(character.ToString()));
            }
        }
    }

    /// <summary>
    /// Appends the surrogate at <paramref name="pos"/> percent-encoded as UTF-8;
    /// a lone surrogate is replaced by U+FFFD.
    /// </summary>
    /// <param name="stringValue">The text containing the surrogate.</param>
    /// <param name="pos">The position of the surrogate.</param>
    /// <param name="output">The target.</param>
    /// <returns><see langword="true"/> if a surrogate pair was consumed.</returns>
    internal static bool AppendSurrogate(string stringValue, int pos, StringBuilder output) {
        if (char.IsHighSurrogate(stringValue[pos])
            && (pos + 1 < stringValue.Length)
            && char.IsLowSurrogate(stringValue[pos + 1])) {
            _ = output.Append(Uri.EscapeDataString(stringValue.Substring(pos, 2)));
            return true;
        }
        _ = output.Append("%EF%BF%BD");
        return false;
    }

    internal static bool IsSurrogate(char cp)
        => (cp >= 0xD800 && cp <= 0xDFFF);

    internal static bool IsIprivate(char cp)
        => (0xE000 <= cp && cp <= 0xF8FF);

    internal static bool IsNotAllowedInUri(char cp)
        => cp < 0x20 || cp == 0x7F
        || cp is '"' or '<' or '>' or '\\' or '^' or '`' or '{' or '|' or '}';

    internal static bool IsUcschar(char cp)
        => (0xA0 <= cp && cp <= 0xD7FF)
        || (0xF900 <= cp && cp <= 0xFDCF)
        || (0xFDF0 <= cp && cp <= 0xFFEF);
}
