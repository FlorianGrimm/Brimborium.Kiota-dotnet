using System.Diagnostics.CodeAnalysis;

namespace Brimborium.UriTemplate;

/// <summary>
/// Resolves a value from URI-template substitutions and appends its expanded
/// representation to a URI-template target.
/// </summary>
public interface IUriTemplateValue {

    /// <summary>
    /// Attempts to resolve this value using the supplied substitutions.
    /// </summary>
    /// <param name="substitutions">Values supplied for the variables in the URI template.</param>
    /// <param name="result">
    /// When this method returns <see langword="true"/>, the resolved value;
    /// otherwise, <see langword="null"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if this value can be resolved; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    bool TryGetValue(
        IReadOnlyDictionary<string, object?> substitutions,
        [MaybeNullWhen(false)] out IODataValue result);

    /// <summary>
    /// Appends this value's URI-template expansion to the target.
    /// </summary>
    /// <param name="prefix">
    /// Optional prefix to append before the value when it produces output.
    /// </param>
    /// <param name="maxChar">
    /// Maximum number of Unicode code points to append, or <c>-1</c> for no limit.
    /// </param>
    /// <param name="replaceReserved">
    /// <see langword="true"/> to percent-encode reserved URI characters;
    /// otherwise, leave them unescaped.
    /// </param>
    /// <param name="target">The target to which the expanded value is appended.</param>
    void AppendValue(string? prefix, int maxChar, bool replaceReserved, in UriTemplateTarget target);
}
