using System.ComponentModel.Design;
using System.Diagnostics.Tracing;
using System.Text;

namespace Brimborium.UriTemplate;

/// <summary>
/// Parses URI-template strings into abstract syntax trees.
/// </summary>
public static class UriTemplateParser {
    /// <summary>
    /// Parses a URI template into a sequence of literal text and expressions.
    /// </summary>
    /// <param name="template">The URI template to parse.</param>
    /// <returns>The parsed template syntax tree.</returns>
    /// <exception cref="ArgumentException">
    /// The template contains invalid syntax, an invalid variable name, or an
    /// invalid prefix modifier.
    /// </exception>
    public static UriTemplateASTSequence Parse(string template) {
        List<UriTemplateASTSequenceChild> resultListItem = [];
        List<UriTemplateASTPlaceholder>? listPlaceholder = default;
        StringBuilder constant = new StringBuilder(template.Length * 2);

        bool toToken = false;
        StringBuilder token = new StringBuilder();

        UriTemplateASTOperator? op = null;
        bool composite = false;
        bool toMaxCharBuffer = false;
        //StringBuilder maxCharBuffer = new StringBuilder(3);
        StringBuilder? maxCharBuffer = default;
        // bool firstToken = true;

        for (int pos = 0; pos < template.Length; pos++) {
            char character = template[pos];
            if (character < 32 || character == 0x7F) {
                throw new ArgumentException($"Control character 0x{(int)character:X2} is not allowed at col:{pos}");
            }
            switch (character) {
                case '{':
                    if (toToken) {
                        throw new ArgumentException($"Double prefix at col:{pos}");
                    } else {
                        if (0 < constant.Length) {
                            resultListItem.Add(
                                new UriTemplateASTConstant(
                                    constant.ToString()
                                    ));
                            _ = constant.Clear();
                        }
                        toToken = true;
                        // firstToken = true;
                    }
                    break;
                case '}':
                    if (toToken) {
                        if (toMaxCharBuffer && maxCharBuffer is { } && maxCharBuffer.Length == 0) {
                            throw new ArgumentException($"Empty prefix after colon at col:{pos}");
                        }

                        var name = CheckVarname(token.ToStringAndClear(), pos);
                        int maxChar;
                        if (maxCharBuffer is null) {
                            maxChar = -1;
                        } else {
                            maxChar = GetMaxChar(maxCharBuffer.ToStringAndClear(), pos);
                        }
                        UriTemplateASTPlaceholder item = new(
                            Name: name,
                            Composite: composite,
                            MaxChar: maxChar);
                        (listPlaceholder ??= []).Add(item);

                        UriTemplateASTOperation uriTemplateASTOperator = new(
                            Operator: op,
                            ListPlaceholder: listPlaceholder.ToArrayAndClear()
                            );
                        resultListItem.Add(uriTemplateASTOperator);

                        toToken = false;
                        op = null;
                        composite = false;
                        toMaxCharBuffer = false;
                    } else {
                        throw new ArgumentException($"Failed to expand token, invalid at col:{pos}");
                    }
                    break;
                case ',':
                    if (toToken) {
                        if (toMaxCharBuffer && maxCharBuffer is { } && maxCharBuffer.Length == 0) {
                            throw new ArgumentException($"Empty prefix after colon at col:{pos}");
                        }
                        var name = CheckVarname(token.ToStringAndClear(), pos);
                        int maxChar;
                        if (maxCharBuffer is null) {
                            maxChar = -1;
                        } else {
                            maxChar = GetMaxChar(maxCharBuffer.ToStringAndClear(), pos);
                        }
                        UriTemplateASTPlaceholder item = new(
                            //Operator: op.GetValueOrDefault(Operator.NO_OP),
                            Name: name,
                            Composite: composite,
                            MaxChar: maxChar
                            );
                        (listPlaceholder ??= []).Add(item);
                        composite = false;
                        toMaxCharBuffer = false;
                        break;
                    }
                    // Intentional fall-through for commas outside the {}
                    goto default;
                default:
                    if (toToken) {
                        if (op == null) {
                            op = GetOperator(character, token, pos);
                        } else if (toMaxCharBuffer) {
                            if (char.IsAsciiDigit(character)) {
                                _ = (maxCharBuffer ?? throw new Exception()).Append(character);
                            } else {
                                throw new ArgumentException($"Illegal character identified in the token at col:{pos}");
                            }
                        } else if (composite) {
                            throw new ArgumentException($"Illegal character after '*' at col:{pos}");
                        } else {
                            if (character == ':') {
                                toMaxCharBuffer = true;
                                if (maxCharBuffer is null) {
                                    maxCharBuffer = new StringBuilder(3);
                                } else {
                                    _ = maxCharBuffer.Clear();
                                }
                            } else if (character == '*') {
                                composite = true;
                            } else if (character == '$' && token.Length == 0) {
                                _ = token.Append(character);
                            } else {
                                ValidateVarchar(character, pos);
                                _ = token.Append(character);
                            }
                        }
                    } else {
                        if (character > 0x7F || char.IsHighSurrogate(character)) {
                            string toEncode;
                            if (char.IsHighSurrogate(character) && pos + 1 < template.Length && char.IsLowSurrogate(template[pos + 1])) {
                                toEncode = template.Substring(pos, 2);
                                pos++;
                            } else {
                                toEncode = character.ToString();
                            }
                            byte[] bytes = Encoding.UTF8.GetBytes(toEncode);
                            foreach (byte b in bytes) {
                                _ = constant.Append($"%{b:X2}");
                            }
                        } else if (character == '%') {
                            if (pos + 2 < template.Length
                                && IsHexDigit(template[pos + 1])
                                && IsHexDigit(template[pos + 2])) {
                                _ = constant.Append(character);
                            } else {
                                _ = constant.Append("%25");
                            }
                        } else if (IsNotAllowedInLiteral(character)) {
                            _ = constant.Append($"%{(int)character:X2}");
                        } else {
                            _ = constant.Append(character);
                        }
                    }
                    break;
            }
        }

        // append the tail
        {
            var tail = constant.ToStringAndClear();
            if (!toToken) {
                if (0 < tail.Length) {
                    resultListItem.Add(
                        new UriTemplateASTConstant(tail));
                }
            } else {
                throw new ArgumentException(
                    $"Unterminated token at col:{template.Length}");
            }
        }

        UriTemplateASTSequence result = new UriTemplateASTSequence(
            [.. resultListItem]
            );
        return result;
    }

    private static int GetMaxChar(string str, int col) {
        if (0 == str.Length) {
            return -1;
        }
        int result;
        try {
            result = int.Parse(str, System.Globalization.CultureInfo.InvariantCulture);
        } catch (Exception error) when (error is FormatException or OverflowException) {
            throw new ArgumentException($"Cannot parse max chars at col:{col}");
        }

        if (str[0] == '0') {
            throw new ArgumentException($"Leading zeros are not allowed in max chars at col:{col}");
        }
        if (result < 1 || result > 9999) {
            throw new ArgumentException($"Max chars must be between 1 and 9999 at col:{col}");
        }

        return result;
    }

    /// <summary>
    /// Validates a URI-template variable name.
    /// </summary>
    /// <param name="token">The variable name to validate.</param>
    /// <param name="col">The template column used in validation error messages.</param>
    /// <returns>The validated variable name.</returns>
    /// <exception cref="ArgumentException">
    /// The name starts or ends with a period, contains consecutive periods, or
    /// contains an invalid percent-encoding sequence.
    /// </exception>
    public static string CheckVarname(string token, int col) {
        if (string.IsNullOrEmpty(token)) {
            throw new ArgumentException($"Variable name cannot be empty at col:{col}");
        }
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
        if (token.StartsWith('.') || token.EndsWith('.'))
#else
        if (token.StartsWith(".") || token.EndsWith("."))
#endif
        {
            throw new ArgumentException($"Variable name cannot start or end with '.' at col:{col}");
        }
        if (token.Contains("..")) {
            throw new ArgumentException($"Variable name cannot contain '..' at col:{col}");
        }
        for (int i = 0; i < token.Length; i++) {
            if (token[i] == '%') {
                if (i + 2 >= token.Length
                    || !IsHexDigit(token[i + 1])
                    || !IsHexDigit(token[i + 2])) {
                    throw new ArgumentException($"Invalid percent encoding in variable name at col:{col}");
                }
            }
        }
        return token;
    }

    private static bool IsHexDigit(char c) {
        return c is '0' or '1' or '2' or '3' or '4' or '5' or '6' or '7' or '8' or '9'
            or 'A' or 'B' or 'C' or 'D' or 'E' or 'F'
            or 'a' or 'b' or 'c' or 'd' or 'e' or 'f'
            ;
        /*
        return (c >= '0' && c <= '9') || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f');
        */
    }

    private static void ValidateVarchar(char c, int col) {
        if (!(char.IsAsciiLetterOrDigit(c) || c is '_' or '.' or '%')) {
            throw new ArgumentException($"Illegal character identified in the token at col:{col}");
        }
    }

    private static bool IsNotAllowedInLiteral(char c)
        => c is ' ' or '"' or '<' or '>' or '\\' or '^' or '`' or '|';

    private static UriTemplateASTOperator GetOperator(char c, StringBuilder token, int col) {
        switch (c) {
            case '+': return UriTemplateASTOperator.Plus;
            case '#': return UriTemplateASTOperator.Hash;
            case '.': return UriTemplateASTOperator.Dot;
            case '/': return UriTemplateASTOperator.Slash;
            case ';': return UriTemplateASTOperator.Semicolon;
            case '?': return UriTemplateASTOperator.Questionmark;
            case '&': return UriTemplateASTOperator.Ampersand;
            default:
                ValidateVarchar(c, col);
                _ = token.Append(c);
                return UriTemplateASTOperator.Noop;
        }
    }
}
