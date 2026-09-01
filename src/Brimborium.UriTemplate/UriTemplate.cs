#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Brimborium.UriTemplate;

public static class UriTemplate {
    public static string Expand(
        string template,
        IReadOnlyDictionary<string, object?> substitutions)
        => UriTemplateCache.Default.Expand(template, substitutions);

    public static UriTemplateASTSequence Parse(
        string template)
        => UriTemplateCache.Default.Parse(template);
}
