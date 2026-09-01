#pragma warning disable IDE0290 // Use primary constructor
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using Microsoft.Extensions.ObjectPool;

using System.Collections.Concurrent;
using System.Text;

namespace Brimborium.UriTemplate;

public sealed class UriTemplateCache {
    private static UriTemplateCache? _Default;

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

    public UriTemplateCache() : this(
        new DefaultObjectPool<StringBuilder>(
            new StringBuilderPooledObjectPolicy {
                InitialCapacity = 4 * 1024,
                MaximumRetainedCapacity = 16 * 1024,
            })
        ) {
    }

    public UriTemplateCache(
        DefaultObjectPool<StringBuilder> stringBuilderPool
        ) {
        this._StringBuilderPool = stringBuilderPool;
    }

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