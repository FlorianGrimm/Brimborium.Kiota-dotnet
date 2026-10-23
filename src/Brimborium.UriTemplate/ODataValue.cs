namespace Brimborium.UriTemplate;

/// <summary>
/// Wraps a CLR value and appends it in its OData literal representation.
/// </summary>
/// <param name="Value">The value to format as an OData literal.</param>
public sealed record ODataValue(object? Value) : IODataValue {
    /// <inheritdoc/>
    public bool AppendValue(in UriTemplateTarget target) {
        return target.Options.AppendValue(this.Value, target.Output);
    }
}
