// ------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------

using Microsoft.Kiota.Abstractions.Serialization;

using System.Text.Json;

namespace Microsoft.Kiota.Serialization.Json {

    /// <summary>
    /// The <see cref="IParseNodeFactory"/> implementation for json content types
    /// </summary>
    public class JsonParseNodeFactory : IParseNodeFactory {
        private readonly KiotaJsonSerializationContext _jsonJsonSerializationContext;

        /// <summary>
        /// The <see cref="JsonParseNodeFactory"/> constructor.
        /// </summary>
        public JsonParseNodeFactory()
            : this(KiotaJsonSerializationContext.Default) {
        }

        /// <summary>
        /// The <see cref="JsonParseNodeFactory"/> constructor. 
        /// </summary>
        /// <param name="jsonJsonSerializationContext">The KiotaSerializationContext to utilize.</param>
        public JsonParseNodeFactory(KiotaJsonSerializationContext jsonJsonSerializationContext) {
            this._jsonJsonSerializationContext = jsonJsonSerializationContext;
        }

        /// <summary>
        /// The valid content type for json
        /// </summary>
        public string ValidContentType { get; } = "application/json";

        /// <summary>
        /// Asynchronously gets the root <see cref="IParseNode"/> of the json to be read.
        /// </summary>
        /// <param name="contentType">The content type of the stream to be parsed</param>
        /// <param name="content">The <see cref="Stream"/> containing json to parse.</param>
        /// <param name="cancellationToken">The cancellation token for the task</param>
        /// <returns>An instance of <see cref="IParseNode"/> for json manipulation</returns>
        public async Task<GetRootParseNodeResult> GetRootParseNodeAsync(
            string contentType, Stream content,
            CancellationToken cancellationToken = default) {
            if (string.IsNullOrEmpty(contentType)) {
                throw new ArgumentNullException(nameof(contentType));
            } else if (!this.ValidContentType.Equals(contentType, StringComparison.OrdinalIgnoreCase)) {
                throw new ArgumentOutOfRangeException($"expected a {this.ValidContentType} content type");
            }

            _ = content ?? throw new ArgumentNullException(nameof(content));

            if (content is MemoryStream memoryStream) {
                // We can use the memory stream directly without copying it to a new memory stream
                // We can use the synchronous Parse method here because we already have the entire content in memory
                // We don't need to Clone the root element because we are not going to dispose the JsonDocument until the caller disposes the GetRootParseNodeResult
                // This is the default case - I believe
#pragma warning disable VSTHRD103 // Call async methods when in an async method
                var jsonDocument = JsonDocument.Parse(memoryStream);
#pragma warning restore VSTHRD103 // Call async methods when in an async method
                var parseNode = new JsonParseNode(jsonDocument.RootElement, this._jsonJsonSerializationContext);
                return new(parseNode, memoryStream, jsonDocument);
            } else {
                // the old way - we have to read the stream into memory first, then parse it
                using var jsonDocument = await JsonDocument.ParseAsync(content, cancellationToken: cancellationToken).ConfigureAwait(false);
                var parseNode = new JsonParseNode(jsonDocument.RootElement.Clone(), this._jsonJsonSerializationContext);
                return new(parseNode, default, default);
            }
        }
    }
}
