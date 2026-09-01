using System.Diagnostics.CodeAnalysis;

namespace Brimborium.UriTemplate;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
public interface IUriTemplateValue {
    bool TryGetValue(
        IReadOnlyDictionary<string, object?> substitutions,
        [MaybeNullWhen(false)] out IODataValue result);

    void AppendValue(string? prefix, int maxChar, bool replaceReserved, in UriTemplateTarget target);
}
