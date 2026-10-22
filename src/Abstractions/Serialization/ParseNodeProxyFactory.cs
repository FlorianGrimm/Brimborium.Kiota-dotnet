// ------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Kiota.Abstractions.Serialization {

    /// <summary>
    /// Proxy factory that allows the composition of before and after callbacks on existing factories.
    /// </summary>
    public abstract class ParseNodeProxyFactory : IParseNodeFactory {

        /// <summary>
        /// The valid content type for the <see cref="ParseNodeProxyFactory"/> instance
        /// </summary>
        public string ValidContentType { get { return this._Concrete.ValidContentType; } }
        private readonly IParseNodeFactory _Concrete;
        private readonly Action<IParsable> _OnBefore;
        private readonly Action<IParsable> _OnAfter;

        /// <summary>
        /// Creates a new proxy factory that wraps the specified concrete factory while composing the before and after callbacks.
        /// </summary>
        /// <param name="concrete">The concrete factory to wrap.</param>
        /// <param name="onBefore">The callback to invoke before the deserialization of any model object.</param>
        /// <param name="onAfter">The callback to invoke after the deserialization of any model object.</param>
        protected ParseNodeProxyFactory(IParseNodeFactory concrete, Action<IParsable> onBefore, Action<IParsable> onAfter) {
            this._Concrete = concrete ?? throw new ArgumentNullException(nameof(concrete));
            this._OnBefore = onBefore;
            this._OnAfter = onAfter;
        }

        /// <summary>
        /// Create a parse node from the given stream and content type.
        /// </summary>
        /// <param name="content">The stream to read the parse node from.</param>
        /// <param name="contentType">The content type of the parse node.</param>
        /// <param name="cancellationToken">The cancellation token for the task.</param>
        /// <returns>A parse node.</returns>
        public async Task<GetRootParseNodeResult> GetRootParseNodeAsync(
            string contentType, 
            Stream content,
            CancellationToken cancellationToken = default) {
            var (node, bufferStream, jsonDocument) = await this._Concrete.GetRootParseNodeAsync(contentType, content, cancellationToken).ConfigureAwait(false);
            var originalBefore = node.OnBeforeAssignFieldValues;
            var originalAfter = node.OnAfterAssignFieldValues;
            node.OnBeforeAssignFieldValues = (x) => {
                this._OnBefore?.Invoke(x);
                originalBefore?.Invoke(x);
            };
            node.OnAfterAssignFieldValues = (x) => {
                this._OnAfter?.Invoke(x);
                originalAfter?.Invoke(x);
            };
            return new (node, bufferStream, jsonDocument);
        }
    }
}
