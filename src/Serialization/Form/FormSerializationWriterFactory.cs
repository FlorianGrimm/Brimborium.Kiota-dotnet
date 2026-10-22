using Microsoft.Kiota.Abstractions.Serialization;

namespace Microsoft.Kiota.Serialization.Form;
/// <summary>Represents a serialization writer factory that can be used to create a form url encoded serialization writer.</summary>
public class FormSerializationWriterFactory : ISerializationWriterFactory {
    /// <inheritdoc/>
    public string ValidContentType => "application/x-www-form-urlencoded";
    /// <inheritdoc/>
    public ISerializationWriter GetSerializationWriter(string contentType) {
        if (string.IsNullOrEmpty(contentType)) {
            throw new ArgumentNullException(nameof(contentType));
        }

        if (!this.ValidContentType.Equals(contentType, StringComparison.OrdinalIgnoreCase)) {
            throw new ArgumentOutOfRangeException($"expected a {this.ValidContentType} content type");
        }

        return new FormSerializationWriter();
    }
}
