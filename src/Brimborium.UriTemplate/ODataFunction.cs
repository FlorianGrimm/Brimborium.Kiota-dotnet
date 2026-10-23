using System.Diagnostics.CodeAnalysis;

namespace Brimborium.UriTemplate;

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
        foreach (var item in ListItem) {
            var itemStart = target.Output.Length;
            var subResult = item.AppendValue(target);
            if (subResult) {
                // OK
                outputLength = target.Output.Length;
                _ = target.Append(',');
            } else {
                target.Output.Length = itemStart;
                result = false;
            }
        }
        target.Output.Length = outputLength;
        _ = target.Append(')');
        return result;
    }
}
