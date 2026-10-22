#pragma warning disable IDE0290 // Use primary constructor

using Microsoft.Extensions.ObjectPool;

using System.Collections.Concurrent;
using System.Text;

namespace Brimborium.UriTemplate;

/// <summary>
/// Caches parsed URI templates and expands them using pooled string builders.
/// </summary>
public sealed class UriTemplateCache {
    private static UriTemplateCache? _Default;

    /// <summary>
    /// Gets or sets the shared default cache used by the static
    /// <see cref="UriTemplate"/> methods.
    /// </summary>
    /// <remarks>
    /// Condition: In your Application you have a fixed static number of endpoints - UriTemplates. In this case you can use the default cache. If you have a dynamic number of endpoints - UriTemplates, you use this instance.
    /// This is because the default cache is a static instance and will never be garbage collected. If you have a dynamic number of endpoints, you should use your own instance of UriTemplateCache, which can be garbage collected when no longer needed.
    /// I assume this is the standard use of this library.
    /// </remarks>
    [System.Diagnostics.CodeAnalysis.AllowNull]
    public static UriTemplateCache Default {
        get {
            {
                if (_Default is { } result) {
                    return result;
                }
            }
            {
                lock (typeof(UriTemplateCache)) {
                    return _Default ??= new();
                }
            }
        }
        set {
            _Default = value;
        }
    }

    private readonly ConcurrentDictionary<string, UriTemplateASTSequence> _CachedItems = new();

    private readonly DefaultObjectPool<StringBuilder> _StringBuilderPool;

    /// <summary>
    /// Initializes a cache with a default pool for expansion string builders.
    /// </summary>
    public UriTemplateCache() : this(
        new DefaultObjectPool<StringBuilder>(
            new StringBuilderPooledObjectPolicy {
                InitialCapacity = 4 * 1024,
                MaximumRetainedCapacity = 16 * 1024,
            })
        ) {
    }

    /// <summary>
    /// Initializes a cache using the specified string builder pool.
    /// </summary>
    /// <param name="stringBuilderPool">The pool used to obtain and return expansion buffers.</param>
    public UriTemplateCache(
        DefaultObjectPool<StringBuilder> stringBuilderPool
        ) {
        this._StringBuilderPool = stringBuilderPool;
    }

    /// <summary>
    /// Parses and expands a URI template using the supplied substitutions.
    /// </summary>
    /// <param name="template">The URI template to expand.</param>
    /// <param name="substitutions">Values to use for variables in the template.</param>
    /// <returns>The expanded URI template.</returns>
    public string Expand(
            string template,
            IReadOnlyDictionary<string, object?> substitutions
        ) {
        var output = this._StringBuilderPool.Get();
        UriTemplateTarget target = new(output);
        _ = this.Parse(template).Expand(substitutions, target);
        var result = output.ToStringAndClear();
        this._StringBuilderPool.Return(output);
        return result;
    }

    /// <summary>
    /// Expands a parsed URI template using the supplied substitutions.
    /// </summary>
    /// <param name="template">The parsed template syntax tree to expand.</param>
    /// <param name="substitutions">Values to use for variables in the template.</param>
    /// <returns>The expanded URI template.</returns>
    public string Expand(
            UriTemplateASTSequence template,
            IReadOnlyDictionary<string, object?> substitutions
        ) {
        var output = this._StringBuilderPool.Get();
        UriTemplateTarget target = new(output);
        _ = template.Expand(substitutions, target);
        var result = output.ToStringAndClear();
        this._StringBuilderPool.Return(output);
        return result;
    }

    /// <summary>
    /// Parses a URI template, returning a cached syntax tree when available.
    /// </summary>
    /// <param name="template">The URI template to parse.</param>
    /// <returns>The parsed template syntax tree.</returns>
    public UriTemplateASTSequence Parse(
        string template
        ) {
        if (this._CachedItems.TryGetValue(template, out var result)) {
            return result;
        } else {
            result = UriTemplateParser.Parse(template);
            _ = this._CachedItems.TryAdd(template, result);
            return result;
        }
    }
}