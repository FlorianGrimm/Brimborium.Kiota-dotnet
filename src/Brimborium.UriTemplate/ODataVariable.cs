using System.Diagnostics.CodeAnalysis;

namespace Brimborium.UriTemplate;

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
