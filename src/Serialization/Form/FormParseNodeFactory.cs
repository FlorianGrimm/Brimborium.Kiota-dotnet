using Microsoft.Kiota.Abstractions.Serialization;

namespace Microsoft.Kiota.Serialization.Form;

/// <summary>
/// The <see cref="IParseNodeFactory"/> implementation for form content types
/// </summary>
public class FormParseNodeFactory : IParseNodeFactory {
    /// <inheritdoc/>
    public string ValidContentType => "application/x-www-form-urlencoded";
    /// <inheritdoc/>
    public async Task<GetRootParseNodeResult> GetRootParseNodeAsync(
        string contentType, Stream content,
        CancellationToken cancellationToken = default) {
        if (string.IsNullOrEmpty(contentType)) {
            throw new ArgumentNullException(nameof(contentType));
        }

        if (!this.ValidContentType.Equals(contentType, StringComparison.OrdinalIgnoreCase)) {
            throw new ArgumentOutOfRangeException($"expected a {this.ValidContentType} content type");
        }

        if (content == null) {
            throw new ArgumentNullException(nameof(content));
        }

        using var reader = new StreamReader(content);
        var rawValue = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        var parseNode = new FormParseNode(rawValue);
        return new(parseNode, default, default);
    }
}
