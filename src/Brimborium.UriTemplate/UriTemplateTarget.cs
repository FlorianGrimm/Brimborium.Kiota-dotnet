using System.Collections;
using System.Runtime.CompilerServices;
using System.Text;

namespace Brimborium.UriTemplate;

/// <summary>
/// Wrap the Output - the url to be generated
/// </summary>
/// <param name="output">the builder for the url</param>
/// <param name="options">How OData date and time literals are written.</param>
public readonly struct UriTemplateTarget(
    StringBuilder output,
    ODataConvertOptions options
    ) {
    /// <summary>
    /// Wrap the Output using the current value of <see cref="ODataConvertOptions.Default"/>.
    /// </summary>
    /// <param name="output">the builder for the url</param>
    public UriTemplateTarget(StringBuilder output)
        : this(output, ODataConvertOptions.Default) {
    }

    /// <summary>
    /// The Url to be build
    /// </summary>
    public readonly StringBuilder Output = output;

    /// <summary>
    /// How OData date and time literals are written.
    /// </summary>
    public readonly ODataConvertOptions Options = options;

    /// <summary>
    /// Appends a character to the URI output.
    /// </summary>
    /// <param name="value">The character to append.</param>
    /// <returns>The output builder, allowing further append operations.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal StringBuilder Append(char value) => this.Output.Append(value);

    /// <summary>
    /// Appends a string to the URI output.
    /// </summary>
    /// <param name="value">The string to append.</param>
    /// <returns>The output builder, allowing further append operations.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal StringBuilder Append(string value) => this.Output.Append(value);

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
    internal static bool IsNativeType(object value) =>
        value is string or bool or int or long or float or double or decimal
            or TimeOnly or DateOnly or DateTime or DateTimeOffset;

    /// <summary>
    /// Convert value of wellknown types.
    /// </summary>
    /// <param name="value">the value to convert</param>
    /// <returns>the value formated</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static string? ConvertNativeTypes(object? value) {
        return value switch {
            null => string.Empty,
            string strValue => strValue,
            bool boolValue => ODataConvert.ConvertBooleanToString(boolValue),
            int number => ODataConvert.ConvertIntToString(number),
            long number => ODataConvert.ConvertLongToString(number),
            // plain values keep "Infinity"; ODataConvert writes the OData "INF"
            float number => number.ToString(System.Globalization.CultureInfo.InvariantCulture),
            double number => number.ToString(System.Globalization.CultureInfo.InvariantCulture),
            decimal number => ODataConvert.ConvertDecimalToString(number),
            TimeOnly timeOnly => ODataConvert.ConvertTimeOnlyToString(timeOnly),
            DateOnly dateOnly => ODataConvert.ConvertDateOnlyToString(dateOnly),
            DateTime dateTime => ODataConvert.ConvertDateTimeToString(dateTime, null),
            DateTimeOffset dateTimeOffset => ODataConvert.ConvertDateTimeOffsetToString(dateTimeOffset),
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
    internal bool AddListValue(UriTemplateASTOperation astOperator, string token, IList value, int maxChar, bool composite) {
        bool first = true;
        if (composite) {
            foreach (object innerValue in value) {
                if (!first) {
                    astOperator.AddSeparator(this.Output);
                }
                this.AddScalarValue(astOperator, token, innerValue, maxChar);
                first = false;
            }
        } else {
            int mark = this.AddNamePrefix(astOperator, token);
            foreach (object innerValue in value) {
                if (!first) {
                    _ = this.Output.Append(',');
                }
                this.AddValueElement(astOperator, token, innerValue, maxChar);
                first = false;
            }
            this.RemoveEmptyNamePrefix(mark);
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
    internal bool AddDictionaryValue(UriTemplateASTOperation astOperator, string token, IDictionary value, int maxChar, bool composite) {
        bool first = true;
        if (maxChar != -1) {
            throw new ArgumentException("Value trimming is not allowed on Dictionaries");
        }
        int mark = composite ? -1 : this.AddNamePrefix(astOperator, token);
        foreach (DictionaryEntry v in value) {
            if (v.Key is not string key) {
                throw new ArgumentException($"Dictionary key must be a string, found {v.Key?.GetType()}");
            }
            if (composite) {
                if (!first) {
                    astOperator.AddSeparator(this.Output);
                }
                this.AddValueElement(astOperator, token, key, maxChar);
                _ = this.Output.Append('=');
            } else {
                if (!first) {
                    _ = this.Output.Append(',');
                }
                this.AddValueElement(astOperator, token, key, maxChar);
                _ = this.Output.Append(',');
            }
            this.AddValueElement(astOperator, token, v.Value, maxChar);
            first = false;
        }
        this.RemoveEmptyNamePrefix(mark);
        return !first;
    }

    /// <summary>
    /// Appends the variable name and <c>=</c> that precede a non-exploded list
    /// or dictionary for the named operators.
    /// </summary>
    /// <param name="astOperator">The expression operator.</param>
    /// <param name="token">The variable name.</param>
    /// <returns>
    /// The output length after the <c>=</c> when it must be removed for an empty
    /// value (<c>;</c> operator); otherwise, <c>-1</c>.
    /// </returns>
    private int AddNamePrefix(UriTemplateASTOperation astOperator, string token) {
        switch (astOperator.Operator) {
            case UriTemplateASTOperator.Questionmark:
            case UriTemplateASTOperator.Ampersand:
                _ = this.Output.Append(token).Append('=');
                return -1;
            case UriTemplateASTOperator.Semicolon:
                _ = this.Output.Append(token).Append('=');
                return this.Output.Length;
            default:
                return -1;
        }
    }

    /// <summary>
    /// Removes the <c>=</c> written by <see cref="AddNamePrefix"/> when no value
    /// followed it.
    /// </summary>
    /// <param name="mark">The value returned by <see cref="AddNamePrefix"/>.</param>
    private void RemoveEmptyNamePrefix(int mark) {
        if (0 <= mark && this.Output.Length == mark) {
            this.Output.Length = mark - 1;
        }
    }

    /// <summary>
    /// Appends a scalar substitution using the formatting rules for the
    /// expression's operator.
    /// </summary>
    /// <param name="astOperator">The expression operator controlling formatting and escaping.</param>
    /// <param name="token">The variable name associated with the value.</param>
    /// <param name="value">The scalar substitution to append.</param>
    /// <param name="maxChar">Maximum Unicode code points to emit, or <c>-1</c> for no limit.</param>
    internal void AddScalarValue(UriTemplateASTOperation astOperator, string token, object value, int maxChar) {
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
    internal void AddValueElement(UriTemplateASTOperation astOperator, string token, object? value, int maxChar) {
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
    internal void AddValueInner(string? prefix, object? value, int maxChar, bool replaceReserved) {
        if (value is null) {
            this.AddValueText(prefix, string.Empty, maxChar, replaceReserved);
            return;
        }

        {
            if (value is IUriTemplateValue uriTemplateValue) {
                if (prefix is not null) {
                    _ = this.Append(prefix);
                }
                uriTemplateValue.AppendValue(null, maxChar, replaceReserved, this);
            } else if (value is IODataValue oDataValue) {
                if (prefix is not null) {
                    _ = this.Append(prefix);
                }
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
    internal void AddValueText(string? prefix, string stringValue, int maxChar, bool replaceReserved) {
        if (maxChar == -1) {
            this.AddValueTextComplete(prefix, stringValue, replaceReserved);
        } else {
            this.AddValueTextWithMaxChar(prefix, stringValue, maxChar, replaceReserved);
        }
    }

    /// <summary>
    /// Appends the whole text after applying the requested URI escaping,
    /// without counting code points.
    /// </summary>
    /// <param name="prefix">An optional prefix to append before non-empty text.</param>
    /// <param name="stringValue">The text to append.</param>
    /// <param name="replaceReserved">
    /// <see langword="true"/> to percent-encode reserved characters;
    /// otherwise, preserve reserved characters.
    /// </param>
    private void AddValueTextComplete(string? prefix, string stringValue, bool replaceReserved) {
        if (stringValue.Length == 0) {
            return;
        }
        _ = this.Output.EnsureCapacity(this.Output.Length + stringValue.Length * 2); // hint to SB

        if (prefix != null) {
            _ = this.Append(prefix);
        }

        for (int pos = 0; pos < stringValue.Length; pos++) {
            pos = AppendValueCharacter(stringValue, pos, replaceReserved, this.Output);
        }
    }

    /// <summary>
    /// Appends at most <paramref name="maxChar"/> code points of the text after
    /// applying the requested URI escaping.
    /// </summary>
    /// <param name="prefix">An optional prefix to append before non-empty text.</param>
    /// <param name="stringValue">The text to append.</param>
    /// <param name="maxChar">Maximum Unicode code points to emit.</param>
    /// <param name="replaceReserved">
    /// <see langword="true"/> to percent-encode reserved characters;
    /// otherwise, preserve reserved characters.
    /// </param>
    private void AddValueTextWithMaxChar(string? prefix, string stringValue, int maxChar, bool replaceReserved) {
        int codePointCount = 0;
        for (int ci = 0; ci < stringValue.Length; ci++) {
            if (char.IsHighSurrogate(stringValue[ci])
                && (ci + 1 < stringValue.Length)
                && char.IsLowSurrogate(stringValue[ci + 1])) {
                ci++;
            }
            codePointCount++;
        }
        int max = Math.Min(maxChar, codePointCount);
        _ = this.Output.EnsureCapacity(this.Output.Length + max * 2); // hint to SB

        if (max > 0 && prefix != null) {
            _ = this.Append(prefix);
        }

        int charCount = 0;
        for (int pos = 0; pos < stringValue.Length && charCount < max; pos++) {
            pos = AppendValueCharacter(stringValue, pos, replaceReserved, this.Output);
            charCount++;
        }
    }

    /// <summary>
    /// Appends the character (or surrogate pair / pct-encoded triplet) at
    /// <paramref name="pos"/> after applying the requested URI escaping.
    /// </summary>
    /// <param name="stringValue">The text containing the character.</param>
    /// <param name="pos">The position of the character.</param>
    /// <param name="replaceReserved">
    /// <see langword="true"/> to percent-encode reserved characters;
    /// otherwise, preserve reserved characters.
    /// </param>
    /// <param name="output">The target</param>
    /// <returns>The position of the last consumed character.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int AppendValueCharacter(string stringValue, int pos, bool replaceReserved, StringBuilder output) {
        char character = stringValue[pos];

        if (UriCharacterUtility.IsSurrogate(character)) {
            if (UriCharacterUtility.AppendSurrogate(stringValue, pos, output)) {
                pos++; // skip the low surrogate
            }
        } else if (character == '%') {
            // keep a valid pct-encoded triplet when reserved characters are allowed
            if (!replaceReserved
                && pos + 2 < stringValue.Length
                && char.IsAsciiHexDigit(stringValue[pos + 1])
                && char.IsAsciiHexDigit(stringValue[pos + 2])) {
                _ = output.Append(stringValue, pos, 3);
                pos += 2;
            } else {
                _ = output.Append("%25");
            }
        } else if (replaceReserved
            || UriCharacterUtility.IsNotAllowedInUri(character)
            || UriCharacterUtility.IsUcschar(character)
            || UriCharacterUtility.IsIprivate(character)) {
            _ = output.Append(Uri.EscapeDataString(character.ToString()));
        } else if (character == ' ') {
            _ = output.Append("%20");
        } else {
            _ = output.Append(character);
        }
        return pos;
    }

    /// <summary>
    /// Appends a string as an OData string literal value, escaping URI-sensitive
    /// characters and doubling apostrophes.
    /// </summary>
    /// <param name="stringValue">The string contents to append after formatted.</param>
    public void AddODataValue(string stringValue)
        => UriCharacterUtility.AppendODataStringContent(stringValue, this.Output);

    /// <summary>
    /// Returns the current URI output as a string.
    /// </summary>
    /// <returns>The contents of the output builder.</returns>
    public override string ToString()
        => this.Output.ToString();
}
