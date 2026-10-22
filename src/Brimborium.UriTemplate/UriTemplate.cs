namespace Brimborium.UriTemplate;

/// <summary>
/// Provides methods to parse URI templates and expand them using substitutions.
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage()]
public static class UriTemplate {

    /// <summary>
    /// Expands a URI template by replacing its variables with values from the
    /// supplied substitutions.
    /// </summary>
    /// <param name="template">The URI template to expand.</param>
    /// <param name="substitutions">Values to use for variables in the template.</param>
    /// <returns>The expanded URI template.</returns>
    public static string Expand(
        string template,
        IReadOnlyDictionary<string, object?> substitutions)
        => UriTemplateCache.Default.Expand(template, substitutions);

    /// <summary>
    /// Parses a URI template into its abstract syntax tree.
    /// </summary>
    /// <param name="template">The URI template to parse.</param>
    /// <returns>The parsed template syntax tree.</returns>
    public static UriTemplateASTSequence Parse(
        string template)
        => UriTemplateCache.Default.Parse(template);
}
