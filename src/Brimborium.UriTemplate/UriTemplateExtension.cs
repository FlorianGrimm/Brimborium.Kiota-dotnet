#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using System.Text;

namespace Brimborium.UriTemplate;

public static class UriTemplateExtension {
    extension(StringBuilder that) {
        public string ToStringAndClear() {
            if (0 == that.Length) {
                return string.Empty;
            } else {
                var result = that.ToString();
                _ = that.Clear();
                return result;
            }
        }
    }
    extension<T>(List<T> that) {
        public T[] ToArrayAndClear() {
            if (that.Count == 0) {
                return Array.Empty<T>();
            } else {
                var result = that.ToArray();
                that.Clear();
                return result;
            }
        }
    }
}