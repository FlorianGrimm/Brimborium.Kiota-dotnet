#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.Design;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Brimborium.UriTemplate;

public interface IODataValue {
    bool AppendValue(in UriTemplateTarget target);
}


public record class ODataExpression(
    IODataValue Value
    ) : IUriTemplateValue, IODataValue {
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

    public void AppendValue(string? prefix, int maxChar, bool replaceReserved, in UriTemplateTarget target) {
        this.AppendValue(target);
    }

    public bool AppendValue(in UriTemplateTarget target) {
        return this.Value.AppendValue(target);
    }

}

public record class ODataExpressionResolved(
    IODataValue Value
    ) : IUriTemplateValue, IODataValue {
    public bool TryGetValue(
        IReadOnlyDictionary<string, object?> substitutions,
        [MaybeNullWhen(false)] out IODataValue result
        ) {
        {
            result = this;
            return true;
        }
    }

    public void AppendValue(string? prefix, int maxChar, bool replaceReserved, in UriTemplateTarget target) {
        if (prefix is { Length: > 0 }) { throw new ArgumentException("should be null", nameof(prefix)); }
        this.AppendValue(target);
    }

    public bool AppendValue(in UriTemplateTarget target) {
        return this.Value.AppendValue(target);
    }

}

public record ODataConstant(string Value) : IUriTemplateValue, IODataValue {
    public bool TryGetValue(
        IReadOnlyDictionary<string, object?> substitutions,
        [MaybeNullWhen(false)] out IODataValue result) {
        result = this;
        return true;
    }
    public void AppendValue(string? prefix, int maxChar, bool replaceReserved, in UriTemplateTarget target) {
        this.AppendValue(target);
    }

    public bool AppendValue(in UriTemplateTarget target) {
        var output = target.Output;
        target.AddODataValue(this.Value);
        return true;
    }
}

public record ODataSequence(
    string Name,
    IODataValue[] ListItem
    ) : IUriTemplateValue, IODataValue {

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

    public void AppendValue(string? prefix, int maxChar, bool replaceReserved, in UriTemplateTarget target) {
        this.AppendValue(target);
    }

    public bool AppendValue(in UriTemplateTarget target) {
        bool result = true;
        foreach (var item in ListItem) {
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

public record ODataOperation(
    IODataValue Left,
    string Name,
    IODataValue Right
    ) : IUriTemplateValue, IODataValue {

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

    public void AppendValue(string? prefix, int maxChar, bool replaceReserved, in UriTemplateTarget target) {
        this.AppendValue(target);
    }

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

public record ODataFunction(
    string Name,
    IODataValue[] ListItem
    ) : IUriTemplateValue, IODataValue {

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

    public void AppendValue(string? prefix, int maxChar, bool replaceReserved, in UriTemplateTarget target) {
        throw new NotImplementedException();
    }

    public bool AppendValue(in UriTemplateTarget target) {
        bool result = true;
        _ = target.Append(this.Name).Append('(');
        bool first = true;
        foreach (var item in ListItem) {
            var subResult = item.AppendValue(target);
            if (subResult) {
                // OK
            } else {
                result = false;
            }
            if (first) {
                first = false;
            } else {
                _ = target.Append(',');
            }
        }
        _ = target.Append(')');
        return result;
    }
}

public record ODataVariable(
    string Name,
    ODataValue? Value
    ) : IUriTemplateValue, IODataValue {

    public bool TryGetValue(
        IReadOnlyDictionary<string, object?> substitutions,
        [MaybeNullWhen(false)] out IODataValue result) {
        if (substitutions.TryGetValue(this.Name, out var substitutionValue)) {
            result = new ODataValue(substitutionValue);
            return true;
        }

        if (this.Value is { } fallbackValue) {
            result = fallbackValue;
            return true;
        }

        result = null;
        return false;
    }

    public void AppendValue(string? prefix, int maxChar, bool replaceReserved, in UriTemplateTarget target) {
        throw new NotImplementedException();
    }

    public bool AppendValue(in UriTemplateTarget target) {
        return false;
    }
}

public record ODataValue(object? Value) : IODataValue {
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
            return false;
        }
    }
}