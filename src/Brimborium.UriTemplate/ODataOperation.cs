using System.Diagnostics.CodeAnalysis;

namespace Brimborium.UriTemplate;

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
        var outputLength = target.Output.Length;
        if (!this.Left.AppendValue(target)) {
            target.Output.Length = outputLength;
            return false;
        }
        _ = target.Append("%20").Append(this.Name).Append("%20");
        if (!this.Right.AppendValue(target)) {
            target.Output.Length = outputLength;
            return false;
        }
        return true;
    }
}
