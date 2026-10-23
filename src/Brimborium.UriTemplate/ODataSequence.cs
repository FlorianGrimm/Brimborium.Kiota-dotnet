using System.Diagnostics.CodeAnalysis;

namespace Brimborium.UriTemplate;

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
        foreach (var item in ListItem) {
            var itemStart = target.Output.Length;
            var subResult = item.AppendValue(target);
            if (subResult) {
                // OK
            } else {
                target.Output.Length = itemStart;
                result = false;
            }
        }
        return result;
    }
}
