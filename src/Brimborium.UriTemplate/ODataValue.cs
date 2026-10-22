using System.Diagnostics.CodeAnalysis;

namespace Brimborium.UriTemplate;

/// <summary>
/// allows a value to formatted and append to the target.
/// </summary>
public interface IODataValue {
    /// <summary>
    /// add the value to the target.Output
    /// </summary>
    /// <param name="target">target for url</param>
    /// <returns>true if successfull</returns>
    bool AppendValue(in UriTemplateTarget target);
}


/// <summary>
/// Wraps an OData value expression and supports resolving any URI-template
/// variables contained in that expression.
/// </summary>
/// <param name="Value">The expression value to resolve or append.</param>
public sealed record class ODataExpression(
    IODataValue Value
    ) : IUriTemplateValue, IODataValue {

    /// <inheritdoc/>
    public bool TryGetValue(
        IReadOnlyDictionary<string, object?> substitutions,
        [MaybeNullWhen(false)] out IODataValue result
        ) {
        {
            if (this.Value is IUriTemplateValue uriTemplateValue) {
                if (uriTemplateValue.TryGetValue(substitutions, out var nextValue)) {
                    result = new ODataExpressionResolved(nextValue);
                    return true;
                } else {
                    result = default;
                    return false;
                }
            }
        }
        {
            result = this.Value;
            return true;
        }
    }

    /// <inheritdoc/>
    public void AppendValue(string? prefix, int maxChar, bool replaceReserved, in UriTemplateTarget target) {
        this.AppendValue(target);
    }

    /// <inheritdoc/>
    public bool AppendValue(in UriTemplateTarget target) {
        return this.Value.AppendValue(target);
    }
}

/// <summary>
/// Represents an OData expression whose URI-template variables have been
/// resolved.
/// </summary>
/// <param name="Value">The resolved expression value to append.</param>
public sealed record class ODataExpressionResolved(
    IODataValue Value
    ) : IUriTemplateValue, IODataValue {
    /// <inheritdoc/>
    public bool TryGetValue(
        IReadOnlyDictionary<string, object?> substitutions,
        [MaybeNullWhen(false)] out IODataValue result
        ) {
        {
            result = this;
            return true;
        }
    }

    /// <inheritdoc/>
    public void AppendValue(string? prefix, int maxChar, bool replaceReserved, in UriTemplateTarget target) {
        if (prefix is { Length: > 0 }) { throw new ArgumentException("should be null", nameof(prefix)); }
        this.AppendValue(target);
    }

    /// <inheritdoc/>
    public bool AppendValue(in UriTemplateTarget target) {
        return this.Value.AppendValue(target);
    }

}

/// <summary>
/// Represents an OData field name, appended as-is rather than as a quoted
/// OData literal.
/// </summary>
/// <param name="Value">The field name.</param>
public sealed record ODataFieldName(string Value) : IUriTemplateValue, IODataValue {
    /// <inheritdoc/>
    public bool TryGetValue(
        IReadOnlyDictionary<string, object?> substitutions,
        [MaybeNullWhen(false)] out IODataValue result) {
        result = this;
        return true;
    }

    /// <inheritdoc/>
    public void AppendValue(string? prefix, int maxChar, bool replaceReserved, in UriTemplateTarget target) {
        this.AppendValue(target);
    }

    /// <inheritdoc/>
    public bool AppendValue(in UriTemplateTarget target) {
        var output = target.Output;
        target.Append(this.Value);
        return true;
    }
}

/// <summary>
/// Represents text that is appended verbatim to the target.
/// </summary>
/// <param name="Value">The raw text to append.</param>
public sealed record ODataRawString(string Value) : IUriTemplateValue, IODataValue {
    /// <inheritdoc/>
    public bool TryGetValue(
        IReadOnlyDictionary<string, object?> substitutions,
        [MaybeNullWhen(false)] out IODataValue result) {
        result = this;
        return true;
    }

    /// <inheritdoc/>
    public void AppendValue(string? prefix, int maxChar, bool replaceReserved, in UriTemplateTarget target) {
        this.AppendValue(target);
    }

    /// <inheritdoc/>
    public bool AppendValue(in UriTemplateTarget target) {
        var output = target.Output;
        target.Append(this.Value);
        return true;
    }
}

/// <summary>
/// Represents an OData literal, which is appended using OData value escaping.
/// </summary>
/// <param name="Value">The literal text.</param>
public sealed record ODataConstant(string Value) : IUriTemplateValue, IODataValue {
    /// <inheritdoc/>
    public bool TryGetValue(
        IReadOnlyDictionary<string, object?> substitutions,
        [MaybeNullWhen(false)] out IODataValue result) {
        result = this;
        return true;
    }

    /// <inheritdoc/>
    public void AppendValue(string? prefix, int maxChar, bool replaceReserved, in UriTemplateTarget target) {
        this.AppendValue(target);
    }

    /// <inheritdoc/>
    public bool AppendValue(in UriTemplateTarget target) {
        var output = target.Output;
        target.AddODataValue(this.Value);
        return true;
    }
}

/// <summary>
/// Represents an ordered sequence of OData values appended consecutively.
/// </summary>
/// <param name="Name">The sequence name retained when resolving its items.</param>
/// <param name="ListItem">The items in the sequence.</param>
public sealed record ODataSequence(
    string Name,
    IODataValue[] ListItem
    ) : IUriTemplateValue, IODataValue {

    /// <inheritdoc/>
    public bool TryGetValue(
        IReadOnlyDictionary<string, object?> substitutions,
        [MaybeNullWhen(false)] out IODataValue result
        ) {
        var thisListItem = this.ListItem;
        IODataValue[]? nextListItem = null;
        for (var i = 0; i < thisListItem.Length; i++) {
            if (thisListItem[i] is IUriTemplateValue uriTemplateValue) {
                if (uriTemplateValue.TryGetValue(substitutions, out var nextItem)) {
                    if (ReferenceEquals(uriTemplateValue, nextItem)) {
                        // no change
                    } else {
                        if (nextListItem is null) {
                            nextListItem = thisListItem.ToArray();
                            nextListItem[i] = nextItem;
                        } else {
                            nextListItem[i] = nextItem;
                        }
                    }
                }
            }
        }
        if (nextListItem is null) {
            result = this;
            return true;
        } else {
            result = new ODataSequence(this.Name, nextListItem);
            return true;
        }
    }

    /// <inheritdoc/>
    public void AppendValue(string? prefix, int maxChar, bool replaceReserved, in UriTemplateTarget target) {
        this.AppendValue(target);
    }

    /// <inheritdoc/>
    public bool AppendValue(in UriTemplateTarget target) {
        bool result = true;
        foreach (var item in this.ListItem) {
            var subResult = item.AppendValue(target);
            if (subResult) {
                // OK
            } else {
                result = false;
            }
        }
        return result;
    }
}

/// <summary>
/// Represents a delimited list of OData values.
/// </summary>
/// <param name="Name">The list name retained when resolving its items.</param>
/// <param name="Seperation">The text placed between successfully appended items.</param>
/// <param name="ListItem">The items in the list.</param>
public sealed record ODataList(
    string Name,
    string Seperation,
    IODataValue[] ListItem
    ) : IUriTemplateValue, IODataValue {

    /// <inheritdoc/>
    public bool TryGetValue(
        IReadOnlyDictionary<string, object?> substitutions,
        [MaybeNullWhen(false)] out IODataValue result
        ) {
        var thisListItem = this.ListItem;
        IODataValue[]? nextListItem = null;
        for (var i = 0; i < thisListItem.Length; i++) {
            if (thisListItem[i] is IUriTemplateValue uriTemplateValue) {
                if (uriTemplateValue.TryGetValue(substitutions, out var nextItem)) {
                    if (ReferenceEquals(uriTemplateValue, nextItem)) {
                        // no change
                    } else {
                        if (nextListItem is null) {
                            nextListItem = thisListItem.ToArray();
                            nextListItem[i] = nextItem;
                        } else {
                            nextListItem[i] = nextItem;
                        }
                    }
                }
            }
        }
        if (nextListItem is null) {
            result = this;
            return true;
        } else {
            result = new ODataList(this.Name, this.Seperation, nextListItem);
            return true;
        }
    }

    /// <inheritdoc/>
    public void AppendValue(string? prefix, int maxChar, bool replaceReserved, in UriTemplateTarget target) {
        this.AppendValue(target);
    }

    /// <inheritdoc/>
    public bool AppendValue(in UriTemplateTarget target) {
        bool result = true;
        int lastSeperate = -1;
        foreach (var item in this.ListItem) {
            var subResult = item.AppendValue(target);
            if (subResult) {
                // OK
                lastSeperate = target.Output.Length;
                target.Append(this.Seperation);
            } else {
                result = false;
            }
        }
        if (0 <= lastSeperate) {
            target.Output.Length = lastSeperate;
        }
        return result;
    }
}

/// <summary>
/// Represents a binary OData operation with a left operand, operator name,
/// and right operand.
/// </summary>
/// <param name="Left">The left operand.</param>
/// <param name="Name">The operation name.</param>
/// <param name="Right">The right operand.</param>
public sealed record ODataOperation(
    IODataValue Left,
    string Name,
    IODataValue Right
    ) : IUriTemplateValue, IODataValue {

    /// <inheritdoc/>
    public bool TryGetValue(
        IReadOnlyDictionary<string, object?> substitutions,
        [MaybeNullWhen(false)] out IODataValue result) {
        IODataValue nextLeft;
        bool changed = false;
        if (this.Left is IUriTemplateValue uriTemplateValueLeft) {
            if (uriTemplateValueLeft.TryGetValue(substitutions, out var innerLeft)) {
                if (ReferenceEquals(uriTemplateValueLeft, innerLeft)) {
                    nextLeft = innerLeft;
                    // no change
                } else {
                    nextLeft = innerLeft;
                    changed = true;
                }
            } else {
                result = null;
                return false;
            }
        } else {
            nextLeft = this.Left;
        }
        IODataValue nextRight;
        if (this.Right is IUriTemplateValue uriTemplateValueRight) {
            if (uriTemplateValueRight.TryGetValue(substitutions, out var innerRight)) {
                if (ReferenceEquals(uriTemplateValueRight, innerRight)) {
                    nextRight = innerRight;
                    // no change
                } else {
                    nextRight = innerRight;
                    changed = true;
                }
            } else {
                result = null;
                return false;
            }
        } else {
            nextRight = this.Right;
        }
        if (changed) {
            result = new ODataOperation(nextLeft, this.Name, nextRight);
            return true;
        } else {
            result = this;
            return true;
        }
    }

    /// <inheritdoc/>
    public void AppendValue(string? prefix, int maxChar, bool replaceReserved, in UriTemplateTarget target) {
        this.AppendValue(target);
    }

    /// <inheritdoc/>
    public bool AppendValue(in UriTemplateTarget target) {
        bool result = true;
        var subResultLeft = this.Left.AppendValue(target);
        if (!subResultLeft) { result = false; }
        _ = target.Append("%20").Append(this.Name).Append("%20");
        var subResultRight = this.Right.AppendValue(target);
        if (!subResultRight) { result = false; }
        return result;
    }
}

/// <summary>
/// Represents an OData function call with an ordered set of arguments.
/// </summary>
/// <param name="Name">The function name.</param>
/// <param name="ListItem">The function arguments.</param>
public sealed record ODataFunction(
    string Name,
    IODataValue[] ListItem
    ) : IUriTemplateValue, IODataValue {

    /// <inheritdoc/>
    public bool TryGetValue(
        IReadOnlyDictionary<string, object?> substitutions,
        [MaybeNullWhen(false)] out IODataValue result) {
        var thisListItem = this.ListItem;
        IODataValue[]? nextListItem = null;
        for (int i = 0; i < thisListItem.Length; i++) {
            if (thisListItem[i] is IUriTemplateValue uriTemplateValue) {
                if (uriTemplateValue.TryGetValue(substitutions, out var nextItem)) {
                    if (ReferenceEquals(uriTemplateValue, nextItem)) {
                        // no change
                    } else {
                        if (nextListItem is null) {
                            nextListItem = thisListItem.ToArray();
                            nextListItem[i] = nextItem;
                        } else {
                            nextListItem[i] = nextItem;
                        }
                    }
                } else {
                    result = null;
                    return false;
                }
            }
        }
        if (nextListItem is null) {
            result = this;
            return true;
        } else {
            result = new ODataFunction(this.Name, nextListItem);
            return true;
        }
    }

    /// <inheritdoc/>
    public void AppendValue(string? prefix, int maxChar, bool replaceReserved, in UriTemplateTarget target) {
        this.AppendValue(target);
    }

    /// <inheritdoc/>
    public bool AppendValue(in UriTemplateTarget target) {
        bool result = true;
        _ = target.Append(this.Name).Append('(');
        var outputLength = target.Output.Length;
        foreach (var item in this.ListItem) {
            var subResult = item.AppendValue(target);
            outputLength = target.Output.Length;
            if (subResult) {
                // OK
                _ = target.Append(',');
            } else {
                result = false;
            }
        }
        target.Output.Length = outputLength;
        _ = target.Append(')');
        return result;
    }
}

/// <summary>
/// Represents a URI-template variable used as an OData value, with an optional
/// fallback value.
/// </summary>
/// <param name="Name">The substitution key to resolve.</param>
/// <param name="Value">The value to use when the substitution is not supplied.</param>
public sealed record ODataVariable(
    string Name,
    ODataValue? Value
    ) : IUriTemplateValue, IODataValue {

    /// <inheritdoc/>
    public bool TryGetValue(
        IReadOnlyDictionary<string, object?> substitutions,
        [MaybeNullWhen(false)] out IODataValue result) {
        if (substitutions.TryGetValue(this.Name, out var substitutionValue)) {
            if (substitutionValue is ODataValue oDataValue) {
                result = oDataValue;
                return true;
            } else {
                result = new ODataValue(substitutionValue);
                return true;
            }
        }

        if (this.Value is { } fallbackValue) {
            result = fallbackValue;
            return true;
        }

        result = null;
        return false;
    }

    /// <inheritdoc/>
    public void AppendValue(string? prefix, int maxChar, bool replaceReserved, in UriTemplateTarget target) {
        this.AppendValue(target);
    }

    /// <inheritdoc/>
    public bool AppendValue(in UriTemplateTarget target) {
        return false;
    }
}

/// <summary>
/// Wraps a CLR value and appends it in its OData literal representation.
/// </summary>
/// <param name="Value">The value to format as an OData literal.</param>
public sealed record ODataValue(object? Value) : IODataValue {
    /// <inheritdoc/>
    public bool AppendValue(in UriTemplateTarget target) {
        var output = target.Output;
        if (this.Value is null) {
            _ = output.Append("null");
            return true;
        } else if (this.Value is string stringValue) {
            _ = output.Append('\'');
            target.AddODataValue(stringValue);
            _ = output.Append('\'');
            return true;
        } else if (this.Value is int intValue) {
            _ = target.Append(intValue.ToString());

            return true;
        } else if (this.Value is long longValue) {
            _ = target.Append(longValue.ToString());
            return true;
        } else if (this.Value is float floatValue) {
            _ = target.Append(floatValue.ToString(System.Globalization.CultureInfo.InvariantCulture));
            return true;
        } else if (this.Value is double doubleValue) {
            _ = target.Append(doubleValue.ToString(System.Globalization.CultureInfo.InvariantCulture));
            return true;
        } else if (this.Value is decimal decimalValue) {
            _ = target.Append(decimalValue.ToString(System.Globalization.CultureInfo.InvariantCulture));
            return true;
        } else if (this.Value is DateTimeOffset dateTimeOffsetValue) {
            _ = target.Append($"'{dateTimeOffsetValue:O}'");
            return true;
        } else if (this.Value is DateTime dateTimeValue) {
            _ = target.Append($"'{dateTimeValue:O}'");
            return true;
        } else if (this.Value is DateOnly dateOnlyValue) {
            _ = target.Append($"'{dateOnlyValue:yyyy-MM-dd}'");
            return true;
        } else if (this.Value is TimeOnly timeOnlyValue) {
            _ = target.Append($"'{timeOnlyValue:hh:mm:ss}'");
            return true;
        } else if (this.Value is bool boolValue) {
            if (boolValue) {
                _ = target.Append("true");
            } else {
                _ = target.Append("false");
            }
            return true;
        } else {
            throw new NotSupportedException(this.Value.GetType().Name);
            // return false;
        }
    }
}