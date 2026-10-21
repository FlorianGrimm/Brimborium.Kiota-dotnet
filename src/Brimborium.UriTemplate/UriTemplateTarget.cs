using System.Collections;
using System.Runtime.CompilerServices;
using System.Text;

namespace Brimborium.UriTemplate;

/// <summary>
/// Wrap the Output - the url to be generated
/// </summary>
/// <param name="output">the builder for the url</param>
public readonly struct UriTemplateTarget(
    StringBuilder output
    ) {
    private readonly StringBuilder _ReservedBuffer = new(3);

    /// <summary>
    /// The Url to be build
    /// </summary>
    public readonly StringBuilder Output = output;

    /// <summary>
    /// Appends a character to the URI output.
    /// </summary>
    /// <param name="value">The character to append.</param>
    /// <returns>The output builder, allowing further append operations.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public StringBuilder Append(char value) => this.Output.Append(value);

    /// <summary>
    /// Appends a string to the URI output.
    /// </summary>
    /// <param name="value">The string to append.</param>
    /// <returns>The output builder, allowing further append operations.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public StringBuilder Append(string value) => this.Output.Append(value);

    /// <summary>
    /// Determines whether a value uses one of the built-in scalar types
    /// supported by URI-template expansion.
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <returns>
    /// <see langword="true"/> if the value is a supported native type;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNativeType(object value) =>
        value is string or bool or int or long or float or double or decimal
            or TimeOnly or DateOnly or DateTime or DateTimeOffset;

    /// <summary>
    /// Convert value of wellknown types.
    /// </summary>
    /// <param name="value">the value to convert</param>
    /// <returns>the value formated</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string? ConvertNativeTypes(object? value) {
        return value switch {
            null => string.Empty,
            string strValue => strValue,
            bool boolValue => boolValue ? "true" : "false",
            int number => number.ToString(System.Globalization.CultureInfo.InvariantCulture),
            long number => number.ToString(System.Globalization.CultureInfo.InvariantCulture),
            float number => number.ToString(System.Globalization.CultureInfo.InvariantCulture),
            double number => number.ToString(System.Globalization.CultureInfo.InvariantCulture),
            decimal number => number.ToString(System.Globalization.CultureInfo.InvariantCulture),
            TimeOnly timeOnly => timeOnly.ToString("hh:mm:ss"),
            DateOnly dateOnly => dateOnly.ToString("yyyy-MM-dd"),
            DateTime dateTime => dateTime.ToString("O"),
            DateTimeOffset dateTimeOffset => dateTimeOffset.ToString("O"),
            _ => null
            //_ => throw new ArgumentException($"Illegal class passed as substitution, found {value.GetType()}"),
        };
    }

    /// <summary>
    /// Appends an <see cref="IList"/> substitution according to the expression's
    /// operator and composite modifier.
    /// </summary>
    /// <param name="astOperator">The expression operator controlling separators and escaping.</param>
    /// <param name="token">The variable name associated with the list.</param>
    /// <param name="value">The list substitution to append.</param>
    /// <param name="maxChar">Maximum code points for each scalar value, or <c>-1</c> for no limit.</param>
    /// <param name="composite">Whether each list item is expanded as a separate composite value.</param>
    /// <returns><see langword="true"/> if the list contains at least one item.</returns>
    public bool AddListValue(UriTemplateASTOperation astOperator, string token, IList value, int maxChar, bool composite) {
        bool first = true;
        foreach (object innerValue in value) {
            if (first) {
                this.AddScalarValue(astOperator, token, innerValue, maxChar);
                first = false;
            } else {
                if (composite) {
                    astOperator.AddSeparator(this.Output);
                    this.AddScalarValue(astOperator, token, innerValue, maxChar);
                } else {
                    _ = this.Output.Append(',');
                    this.AddValueElement(astOperator, token, innerValue, maxChar);
                }
            }
        }
        return !first;
    }

    /// <summary>
    /// Appends an <see cref="IDictionary"/> substitution according to the
    /// expression's operator and composite modifier.
    /// </summary>
    /// <param name="astOperator">The expression operator controlling separators and escaping.</param>
    /// <param name="token">The variable name associated with the dictionary.</param>
    /// <param name="value">The dictionary substitution to append.</param>
    /// <param name="maxChar">Must be <c>-1</c>; dictionary values cannot be truncated.</param>
    /// <param name="composite">Whether dictionary entries use composite key/value formatting.</param>
    /// <returns><see langword="true"/> if the dictionary contains at least one entry.</returns>
    /// <exception cref="ArgumentException"><paramref name="maxChar"/> is not <c>-1</c>.</exception>
    public bool AddDictionaryValue(UriTemplateASTOperation astOperator, string token, IDictionary value, int maxChar, bool composite) {
        bool first = true;
        if (maxChar != -1) {
            throw new ArgumentException("Value trimming is not allowed on Dictionaries");
        }
        foreach (DictionaryEntry v in value) {
            if (composite) {
                if (!first) {
                    astOperator.AddSeparator(this.Output);
                }
                this.AddValueElement(astOperator, token, (string)v.Key, maxChar);
                _ = this.Output.Append('=');
            } else {
                if (first) {
                    this.AddScalarValue(astOperator, token, (string)v.Key, maxChar);
                } else {
                    _ = this.Output.Append(',');
                    this.AddValueElement(astOperator, token, (string)v.Key, maxChar);
                }
                _ = this.Output.Append(',');
            }
            this.AddValueElement(astOperator, token, v.Value, maxChar);
            first = false;
        }
        return !first;
    }

    /// <summary>
    /// Appends a scalar substitution using the formatting rules for the
    /// expression's operator.
    /// </summary>
    /// <param name="astOperator">The expression operator controlling formatting and escaping.</param>
    /// <param name="token">The variable name associated with the value.</param>
    /// <param name="value">The scalar substitution to append.</param>
    /// <param name="maxChar">Maximum Unicode code points to emit, or <c>-1</c> for no limit.</param>
    public void AddScalarValue(UriTemplateASTOperation astOperator, string token, object value, int maxChar) {
        if (astOperator.Operator is { } op) {
            switch (op) {
                case UriTemplateASTOperator.Plus:
                case UriTemplateASTOperator.Hash:
                    this.AddValueInner(null, value, maxChar, false);
                    return;
                case UriTemplateASTOperator.Questionmark:
                case UriTemplateASTOperator.Ampersand:
                    _ = this.Append(token).Append('=');
                    this.AddValueInner(null, value, maxChar, true);
                    return;
                case UriTemplateASTOperator.Semicolon:
                    _ = this.Output.Append(token);
                    this.AddValueInner("=", value, maxChar, true);
                    return;
                case UriTemplateASTOperator.Dot:
                case UriTemplateASTOperator.Slash:
                case UriTemplateASTOperator.Noop:
                    this.AddValueInner(null, value, maxChar, true);
                    return;
            }
        }
    }

    /// <summary>
    /// Appends one element of a list or dictionary substitution using the
    /// expression's escaping rules.
    /// </summary>
    /// <param name="astOperator">The expression operator controlling escaping.</param>
    /// <param name="token">The variable name associated with the element.</param>
    /// <param name="value">The element to append.</param>
    /// <param name="maxChar">Maximum Unicode code points to emit, or <c>-1</c> for no limit.</param>
    public void AddValueElement(UriTemplateASTOperation astOperator, string token, object? value, int maxChar) {
        if (astOperator.Operator is { } op) {
            switch (op) {
                case UriTemplateASTOperator.Plus:
                case UriTemplateASTOperator.Hash:
                    this.AddValueInner(null, value, maxChar, false);
                    return;
                case UriTemplateASTOperator.Questionmark:
                case UriTemplateASTOperator.Ampersand:
                case UriTemplateASTOperator.Semicolon:
                case UriTemplateASTOperator.Dot:
                case UriTemplateASTOperator.Slash:
                case UriTemplateASTOperator.Noop:
                    this.AddValueInner(null, value, maxChar, true);
                    return;
            }
        }
    }

    /// <summary>
    /// Appends a supported substitution value, formatting it as text and
    /// applying URI escaping as requested.
    /// </summary>
    /// <param name="prefix">An optional prefix to append before non-empty text.</param>
    /// <param name="value">The substitution value to format.</param>
    /// <param name="maxChar">Maximum Unicode code points to emit, or <c>-1</c> for no limit.</param>
    /// <param name="replaceReserved">
    /// <see langword="true"/> to percent-encode reserved characters;
    /// otherwise, preserve reserved characters.
    /// </param>
    /// <exception cref="ArgumentException">The value is not a supported substitution type.</exception>
    public void AddValueInner(string? prefix, object? value, int maxChar, bool replaceReserved) {
        if (value is null) {
            this.AddValueText(prefix, string.Empty, maxChar, replaceReserved);
            return;
        }

        {
            if (value is IUriTemplateValue uriTemplateValue) {
                uriTemplateValue.AppendValue(prefix, maxChar, replaceReserved, this);
            } else if (value is IODataValue oDataValue) {
                _ = oDataValue.AppendValue(this);
            } else if (ConvertNativeTypes(value) is { } stringValue) {
                this.AddValueText(prefix, stringValue, maxChar, replaceReserved);
            } else {
                throw new ArgumentException($"Illegal class passed as substitution, found {value.GetType()}");
            }
        }
    }

    /// <summary>
    /// Appends text after applying the requested URI escaping and code-point
    /// limit.
    /// </summary>
    /// <param name="prefix">An optional prefix to append before non-empty text.</param>
    /// <param name="stringValue">The text to append.</param>
    /// <param name="maxChar">Maximum Unicode code points to emit, or <c>-1</c> for no limit.</param>
    /// <param name="replaceReserved">
    /// <see langword="true"/> to percent-encode reserved characters;
    /// otherwise, preserve reserved characters.
    /// </param>
    public void AddValueText(string? prefix, string stringValue, int maxChar, bool replaceReserved) {
        int codePointCount = 0;
        bool foundSurrogate = false;
        for (int ci = 0; ci < stringValue.Length; ci++) {
            if (char.IsHighSurrogate(stringValue[ci])
                && (ci + 1 < stringValue.Length)
                && char.IsLowSurrogate(stringValue[ci + 1])) {
                ci++;
                foundSurrogate = true;
            }
            codePointCount++;
        }
        int max = (maxChar != -1) ? Math.Min(maxChar, codePointCount) : codePointCount;
        _ = this.Output.EnsureCapacity(this.Output.Length + max * 2); // hint to SB
        bool toReserved = false;

        if (max > 0 && prefix != null) {
            _ = this.Append(prefix);
        }

        int charCount = 0;
        for (int pos = 0; pos < stringValue.Length && charCount < max; pos++) {
            char character = stringValue[pos];

            if (character == '%' && !replaceReserved) {
                _ = _ReservedBuffer.Clear();
                toReserved = true;
            }

            string toAppend;
            if (foundSurrogate && IsSurrogate(character)) {
                toAppend = Uri.EscapeDataString(char.ConvertFromUtf32(char.ConvertToUtf32(stringValue, pos)));
                pos++; // skip the low surrogate
            } else if (replaceReserved || IsUcschar(character) || IsIprivate(character)) {
                toAppend = Uri.EscapeDataString(character.ToString());
            } else {
                if (toReserved) {
                    toAppend = character.ToString();
                } else {
                    if (character == ' ') {
                        _ = this.Output.Append("%20");
                    } else if (character == '%') {
                        _ = this.Output.Append("%25");
                    } else {
                        _ = this.Output.Append(character);
                    }
                    continue;
                }
            }

            if (toReserved) {
                _ = _ReservedBuffer.Append(toAppend);

                if (_ReservedBuffer.Length == 3) {
                    bool isEncoded = false;

                    var original = _ReservedBuffer.ToStringAndClear();
                    try {
                        isEncoded = !original.Equals(Uri.UnescapeDataString(original));
                    } catch (Exception) {
                        // ignore
                    }

                    if (isEncoded) {
                        _ = this.Output.Append(original);
                    } else {
                        _ = this.Output.Append("%25");
                        // only if !replaceReserved
                        _ = this.Output.Append(original.AsSpan(1, 2));
                    }
                    toReserved = false;
                }
            } else {
                if (character == ' ') {
                    _ = this.Output.Append("%20");
                } else if (character == '%') {
                    _ = this.Output.Append("%25");
                } else {
                    _ = this.Output.Append(toAppend);
                }
            }

            charCount++;
        }

        if (toReserved) {
            _ = this.Output
                .Append("%25")
                .Append(_ReservedBuffer.ToString(1, _ReservedBuffer.Length - 1));
        }
    }

    /// <summary>
    /// Appends a string as an OData string literal value, escaping URI-sensitive
    /// characters and doubling apostrophes.
    /// </summary>
    /// <param name="stringValue">The string contents to append.</param>
    public void AddODataValue(string stringValue) {
        int codePointCount = 0;
        bool foundSurrogate = false;
        for (int ci = 0; ci < stringValue.Length; ci++) {
            if (char.IsHighSurrogate(stringValue[ci])
                && (ci + 1 < stringValue.Length)
                && char.IsLowSurrogate(stringValue[ci + 1])) {
                ci++;
                foundSurrogate = true;
            }
            codePointCount++;
        }
        int max = codePointCount;
        _ = this.Output.EnsureCapacity(this.Output.Length + max * 2); // hint to SB
        bool toReserved = false;

        int charCount = 0;
        for (int pos = 0; pos < stringValue.Length && charCount < max; pos++) {
            char character = stringValue[pos];

            if (character == '%') {
                _ = _ReservedBuffer.Clear();
                toReserved = true;
            }

            string toAppend;
            if (foundSurrogate && IsSurrogate(character)) {
                toAppend = Uri.EscapeDataString(char.ConvertFromUtf32(char.ConvertToUtf32(stringValue, pos)));
                pos++; // skip the low surrogate
            } else if (IsUcschar(character) || IsIprivate(character)) {
                toAppend = Uri.EscapeDataString(character.ToString());
            } else {
                if (toReserved) {
                    toAppend = character.ToString();
                } else {
                    if (character == ' ') {
                        _ = this.Output.Append("%20");
                    } else if (character == '%') {
                        _ = this.Output.Append("%25");
                    } else if (character == '\'') {
                        _ = this.Output.Append("\'\'");
                    } else {
                        _ = this.Output.Append(character);
                    }
                    continue;
                }
            }

            if (toReserved) {
                _ = _ReservedBuffer.Append(toAppend);

                if (_ReservedBuffer.Length == 3) {
                    bool isEncoded = false;

                    var original = _ReservedBuffer.ToStringAndClear();
                    try {
                        isEncoded = !original.Equals(Uri.UnescapeDataString(original));
                    } catch (Exception) {
                        // ignore
                    }

                    if (isEncoded) {
                        _ = this.Output.Append(original);
                    } else {
                        _ = this.Output.Append("%25");
                        // only if !replaceReserved
                        _ = this.Output.Append(original.AsSpan(1, 2));
                    }
                    toReserved = false;
                }
            } else {
                if (character == ' ') {
                    _ = this.Output.Append("%20");
                } else if (character == '%') {
                    _ = this.Output.Append("%25");
                } else if (character == '\'') {
                    _ = this.Output.Append("\'\'");
                } else {
                    _ = this.Output.Append(toAppend);
                }
            }

            charCount++;
        }

        if (toReserved) {
            _ = this.Output
                .Append("%25")
                .Append(_ReservedBuffer.ToString(1, _ReservedBuffer.Length - 1));
        }
    }

    /// <summary>
    /// Returns the current URI output as a string.
    /// </summary>
    /// <returns>The contents of the output builder.</returns>
    public override string ToString()
        => this.Output.ToString();

    private static bool IsSurrogate(char cp)
        => (cp >= 0xD800 && cp <= 0xDFFF);

    private static bool IsIprivate(char cp)
        => (0xE000 <= cp && cp <= 0xF8FF);

    private static bool IsUcschar(char cp)
        => (0xA0 <= cp && cp <= 0xD7FF)
        || (0xF900 <= cp && cp <= 0xFDCF)
        || (0xFDF0 <= cp && cp <= 0xFFEF);
}
