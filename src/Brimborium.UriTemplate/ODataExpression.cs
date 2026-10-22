using System.Diagnostics.CodeAnalysis;

namespace Brimborium.UriTemplate;

/// <summary>
/// Wraps an OData value expression and supports resolving any URI-template
/// variables contained in that expression.
/// </summary>
/// <param name="Value">The expression value to resolve or append.</param>
public sealed record class ODataExpression(
    IODataValue Value
    ) : IUriTemplateValue, IODataValue {

    /// <inheritdoc/>
    public bool TryGetValue(
        IReadOnlyDictionary<string, object?> substitutions,
        [MaybeNullWhen(false)] out IODataValue result
        ) {
        {
            if (this.Value is IUriTemplateValue uriTemplateValue) {
                if (uriTemplateValue.TryGetValue(substitutions, out var nextValue)) {
                    result = new ODataExpressionResolved(nextValue);
                    return true;
                } else {
                    result = default;
                    return false;
                }
            }
        }
        {
            result = this.Value;
            return true;
        }
    }

    /// <inheritdoc/>
    [ExcludeFromCodeCoverage(Justification = "Should not be called")]
    public void AppendValue(string? prefix, int maxChar, bool replaceReserved, in UriTemplateTarget target) {
        this.AppendValue(target);
    }

    /// <inheritdoc/>
    [ExcludeFromCodeCoverage(Justification = "Should not be called")]
    public bool AppendValue(in UriTemplateTarget target) {
        return this.Value.AppendValue(target);
    }
}
