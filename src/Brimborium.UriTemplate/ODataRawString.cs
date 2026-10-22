using System.Diagnostics.CodeAnalysis;

namespace Brimborium.UriTemplate;

/// <summary>
/// Represents text that is appended verbatim to the target.
/// So do your homework and make sure that the text is properly escaped for OData values.
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
