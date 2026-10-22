// ------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------

using Microsoft.Kiota.Http.HttpClientLibrary.Extensions;
using Microsoft.Kiota.Http.HttpClientLibrary.Middleware.Options;

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Kiota.Http.HttpClientLibrary.Middleware;

/// <summary>
/// The Body Inspection Handler allows the developer to inspect the body of the request and response.
/// </summary>
public class BodyInspectionHandler : DelegatingHandler {
    private readonly BodyInspectionHandlerOption _DefaultOptions;

    /// <summary>
    /// Create a new instance of <see cref="BodyInspectionHandler"/>
    /// </summary>
    /// <param name="defaultOptions">Default options to apply to the handler</param>
    public BodyInspectionHandler(BodyInspectionHandlerOption? defaultOptions = null) {
        this._DefaultOptions = defaultOptions ?? new BodyInspectionHandlerOption();
    }

    /// <inheritdoc/>
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    ) {
        if (request == null) {
            throw new ArgumentNullException(nameof(request));
        }

        var options = request.GetRequestOption<BodyInspectionHandlerOption>() ?? this._DefaultOptions;

        Activity? activity;
        if (request.GetRequestOption<ObservabilityOptions>() is { } obsOptions) {
            var activitySource = ActivitySourceRegistry.DefaultInstance.GetOrCreateActivitySource(
                obsOptions.TracerInstrumentationName
            );
            activity = activitySource?.StartActivity(
                $"{nameof(BodyInspectionHandler)}_{nameof(SendAsync)}"
            );
            activity?.SetTag("com.microsoft.kiota.handler.bodyInspection.enable", true);
        } else {
            activity = null;
        }
        try {
            if (options.InspectRequestBody) {
                options.RequestBody = await CopyToStreamAsync(request.Content, cancellationToken)
                    .ConfigureAwait(false);
            }
            var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (options.InspectResponseBody) {
                options.ResponseBody = await CopyToStreamAsync(response.Content, cancellationToken)
                    .ConfigureAwait(false);
            }

            return response;
        } finally {
            activity?.Dispose();
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        [return: NotNullIfNotNull(nameof(httpContent))]
#endif
        static async Task<Stream> CopyToStreamAsync(
            HttpContent? httpContent,
            CancellationToken cancellationToken
        ) {
            if (httpContent is null or { Headers.ContentLength: 0 }) {
                return Stream.Null;
            }

            var stream = new MemoryStream();
            await httpContent.LoadIntoBufferAsync().ConfigureAwait(false);

#if NET5_0_OR_GREATER
            await httpContent.CopyToAsync(stream, cancellationToken).ConfigureAwait(false);
#else
            await httpContent.CopyToAsync(stream).ConfigureAwait(false);
#endif

            if (stream.CanSeek) {
                stream.Position = 0;
            }

            return stream;
        }
    }
}
