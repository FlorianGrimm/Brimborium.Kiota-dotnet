using System.Diagnostics.CodeAnalysis;

namespace Brimborium.UriTemplate;

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
