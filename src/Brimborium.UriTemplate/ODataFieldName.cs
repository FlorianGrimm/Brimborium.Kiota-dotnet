using System.Diagnostics.CodeAnalysis;

namespace Brimborium.UriTemplate;

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
        target.AddValueText(null, this.Value, -1, true);
        return true;
    }
}
