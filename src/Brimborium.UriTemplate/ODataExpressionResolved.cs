using System.Diagnostics.CodeAnalysis;

namespace Brimborium.UriTemplate;

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
