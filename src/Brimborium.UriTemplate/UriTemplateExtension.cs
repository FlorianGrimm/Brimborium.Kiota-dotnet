using System.Text;

namespace Brimborium.UriTemplate;

/// <summary>
/// Provides extension methods for extracting values from and clearing
/// commonly used mutable collections.
/// </summary>
public static class UriTemplateExtension {
    extension(StringBuilder that) {
        /// <summary>
        /// Returns the current contents of the string builder and clears it.
        /// </summary>
        /// <returns>The string builder's contents, or <see cref="string.Empty"/> if empty.</returns>
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
        /// <summary>
        /// Copies the list items to an array and clears the list.
        /// </summary>
        /// <returns>An array containing the list items, or an empty array if the list is empty.</returns>
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