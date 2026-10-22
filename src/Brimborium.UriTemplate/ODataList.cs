using System.Diagnostics.CodeAnalysis;

namespace Brimborium.UriTemplate;

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
        foreach (var item in ListItem) {
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
