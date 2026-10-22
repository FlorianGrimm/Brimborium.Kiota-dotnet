// ------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Abstractions.Extensions;
using Microsoft.Kiota.Abstractions.Helpers;
using Microsoft.Kiota.Abstractions.Serialization;
using Microsoft.Kiota.Abstractions.Store;
using Microsoft.Kiota.Http.HttpClientLibrary.Middleware;

using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text.RegularExpressions;

namespace Microsoft.Kiota.Http.HttpClientLibrary {

    /// <summary>
    /// The <see cref="IRequestAdapter"/> implementation for sending requests.
    /// </summary>
    public class HttpClientRequestAdapter : IRequestAdapter, IDisposable {
        private readonly HttpClient _Client;
        private readonly IAuthenticationProvider _AuthProvider;
        private IParseNodeFactory _PNodeFactory;
        private ISerializationWriterFactory _SWriterFactory;
        private string? _BaseUrl;
        private readonly bool _CreatedClient;
        private readonly ObservabilityOptions _ObsOptions;
        private readonly ActivitySource _ActivitySource;
        private readonly Version? _HttpVersion;
        private static readonly Version _DefaultHttpVersion = HttpVersion.Version20;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpClientRequestAdapter"/> class.
        /// <param name="authenticationProvider">The authentication provider.</param>
        /// <param name="parseNodeFactory">The parse node factory.</param>
        /// <param name="serializationWriterFactory">The serialization writer factory.</param>
        /// <param name="httpClient">The native HTTP client.</param>
        /// <param name="observabilityOptions">The observability options.</param>
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public HttpClientRequestAdapter(IAuthenticationProvider authenticationProvider, IParseNodeFactory? parseNodeFactory, ISerializationWriterFactory? serializationWriterFactory, HttpClient? httpClient, ObservabilityOptions? observabilityOptions)
            : this(authenticationProvider, parseNodeFactory, serializationWriterFactory, httpClient, observabilityOptions, httpVersion: null) {
            // Constructor without HttpVersion for runtime backwards compatibility. Adding a new
            // optional parameter provides *compile-time* backwards compatibility but throws a
            // MissingMethodException at runtime.
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpClientRequestAdapter"/> class.
        /// <param name="authenticationProvider">The authentication provider.</param>
        /// <param name="parseNodeFactory">The parse node factory.</param>
        /// <param name="serializationWriterFactory">The serialization writer factory.</param>
        /// <param name="httpClient">The native HTTP client.</param>
        /// <param name="observabilityOptions">The observability options.</param>
        /// <param name="httpVersion">The HTTP version.</param>
        /// </summary>
        [ActivatorUtilitiesConstructor]
        public HttpClientRequestAdapter(IAuthenticationProvider authenticationProvider, IParseNodeFactory? parseNodeFactory = null, ISerializationWriterFactory? serializationWriterFactory = null, HttpClient? httpClient = null, ObservabilityOptions? observabilityOptions = null, Version? httpVersion = null) {
            this._AuthProvider = authenticationProvider ?? throw new ArgumentNullException(nameof(authenticationProvider));
            this._CreatedClient = httpClient == null;
            this._Client = httpClient ?? KiotaClientFactory.Create();
            this.BaseUrl = this._Client.BaseAddress?.ToString();
            this._PNodeFactory = parseNodeFactory ?? ParseNodeFactoryRegistry.DefaultInstance;
            this._SWriterFactory = serializationWriterFactory ?? SerializationWriterFactoryRegistry.DefaultInstance;
            this._ObsOptions = observabilityOptions ?? new ObservabilityOptions();
            this._ActivitySource = ActivitySourceRegistry.DefaultInstance.GetOrCreateActivitySource(this._ObsOptions.TracerInstrumentationName);
            this._HttpVersion = httpVersion;
        }

        /// <summary>Factory to use to get a serializer for payload serialization</summary>
        public ISerializationWriterFactory SerializationWriterFactory {
            get {
                return this._SWriterFactory;
            }
        }

        /// <summary>
        /// The base url for every request.
        /// </summary>
        public string? BaseUrl {
            get => this._BaseUrl;
            set => this._BaseUrl = value?.TrimEnd('/');
        }
        private static readonly char[] _CharactersToDecodeForUriTemplate = ['$', '.', '-', '~'];
        private static readonly Regex _QueryParametersCleanupRegex = new(@"\{\?[^\}]+}", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Singleline, TimeSpan.FromMilliseconds(100));

        private Activity? StartTracingSpan(RequestInformation requestInfo, string methodName) {
            var decodedUriTemplate = ParametersNameDecodingHandler.DecodeUriEncodedString(requestInfo.UrlTemplate, _CharactersToDecodeForUriTemplate);
            var telemetryPathValue = string.IsNullOrEmpty(decodedUriTemplate) ? string.Empty : _QueryParametersCleanupRegex.Replace(decodedUriTemplate, string.Empty);
            // Also strip literal query strings (e.g., ?@id={@id})
            var questionMarkIndex = telemetryPathValue.IndexOf('?');
            if (questionMarkIndex >= 0) {
                telemetryPathValue = telemetryPathValue.Substring(0, questionMarkIndex);
            }
            var span = this._ActivitySource?.StartActivity($"{methodName} - {telemetryPathValue}");
            _ = (span?.SetTag("url.uri_template", decodedUriTemplate));
            if (!string.IsNullOrEmpty(telemetryPathValue)) {
                var httpRoute = this.GetNormalizedHttpRoute(telemetryPathValue);
                _ = (span?.SetTag("http.route", httpRoute));
            }
            return span;
        }

        /// <summary>
        /// Normalizes the URI template into an HTTP route for observability by removing
        /// the base URL placeholder and prefix, ensuring the route starts with "/".
        /// </summary>
        /// <param name="telemetryPathValue">The URI template path to normalize.</param>
        /// <returns>The normalized HTTP route.</returns>
        internal string GetNormalizedHttpRoute(string telemetryPathValue) {
            const string baseUrlPlaceholder = "{+baseurl}";
            // Optimized path using spans to reduce string allocations
            var span = telemetryPathValue.AsSpan();

            // Remove the base URL placeholder if present
            var placeholderIndex = span.IndexOf(baseUrlPlaceholder.AsSpan(), StringComparison.OrdinalIgnoreCase);
            if (placeholderIndex >= 0) {
                // Build the string without the placeholder in a single allocation
                var withoutPlaceholder = string.Create(
                    telemetryPathValue.Length - baseUrlPlaceholder.Length,
                    (telemetryPathValue, placeholderIndex, baseUrlPlaceholder.Length),
                    static (destination, state) => {
                        var (source, index, length) = state;
                        // Copy the part before the placeholder
                        source.AsSpan(0, index).CopyTo(destination);
                        // Copy the part after the placeholder
                        source.AsSpan(index + length).CopyTo(destination[index..]);
                    });
                span = withoutPlaceholder.AsSpan();
            }

            // Remove the base URL prefix if present (zero-allocation slice)
            if (!string.IsNullOrEmpty(this.BaseUrl) && span.StartsWith(this.BaseUrl.AsSpan(), StringComparison.OrdinalIgnoreCase)) {
                span = span[this.BaseUrl.Length..];
            }

            // Trim whitespace (zero-allocation)
            span = span.Trim();

            // If empty, return "/"
            if (span.IsEmpty) {
                return "/";
            }

            // Trim leading slashes (zero-allocation slice)
            span = span.TrimStart('/');

            // Final allocation - create string with "/" prefix
            return "/" + new string(span);
        }

        /// <summary>
        /// Send a <see cref="RequestInformation"/> instance with a collection instance of <typeparam name="ModelType"></typeparam>
        /// </summary>
        /// <param name="requestInfo">The <see cref="RequestInformation"/> instance to send</param>
        /// <param name="factory">The factory of the response model to deserialize the response into.</param>
        /// <param name="errorMapping">The error factories mapping to use in case of a failed request.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to use for cancelling the request.</param>
        public async Task<IEnumerable<ModelType>?> SendCollectionAsync<ModelType>(RequestInformation requestInfo, ParsableFactory<ModelType> factory, Dictionary<string, ParsableFactory<IParsable>>? errorMapping = default, CancellationToken cancellationToken = default) where ModelType : IParsable {
            using var span = this.StartTracingSpan(requestInfo, nameof(SendCollectionAsync));
            var response = await this.GetHttpResponseMessageAsync(requestInfo, cancellationToken, span).ConfigureAwait(false);
            if (requestInfo.Content is not null) {
                await requestInfo.Content.DisposeAsync().ConfigureAwait(false);
            }

            var responseHandler = GetResponseHandler(requestInfo);
            if (responseHandler == null) {
                try {
                    await this.ThrowIfFailedResponseAsync(response, errorMapping, span, cancellationToken).ConfigureAwait(false);
                    if (shouldReturnNull(response)) {
                        return default;
                    }
                    var (rootNode, bufferstream, jsonDocument) = await this.GetRootParseNodeQAsync(response, cancellationToken).ConfigureAwait(false);
                    using (bufferstream) {
                        using (jsonDocument) {
                            using var spanForDeserialization = this._ActivitySource?.StartActivity(nameof(IParseNode.GetCollectionOfObjectValues));
                            var result = rootNode?.GetCollectionOfObjectValues<ModelType>(factory);
                            SetResponseType(result, span);
                            return result;
                        }
                    }
                } finally {
                    await DrainAsync(response, cancellationToken).ConfigureAwait(false);
                }
            } else {
                span?.AddEvent(new ActivityEvent(EventResponseHandlerInvokedKey));
                return await responseHandler.HandleResponseAsync<HttpResponseMessage, IEnumerable<ModelType>>(response, errorMapping).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Executes the HTTP request specified by the given RequestInformation and returns the deserialized primitive response model collection.
        /// </summary>
        /// <param name="requestInfo">The RequestInformation object to use for the HTTP request.</param>
        /// <param name="errorMapping">The error factories mapping to use in case of a failed request.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to use for cancelling the request.</param>
        /// <returns>The deserialized primitive response model collection.</returns>
        public async Task<IEnumerable<ModelType>?> SendPrimitiveCollectionAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] ModelType>(RequestInformation requestInfo, Dictionary<string, ParsableFactory<IParsable>>? errorMapping = default, CancellationToken cancellationToken = default) {
            using var span = this.StartTracingSpan(requestInfo, nameof(SendPrimitiveCollectionAsync));
            var response = await this.GetHttpResponseMessageAsync(requestInfo, cancellationToken, span).ConfigureAwait(false);
            if (requestInfo.Content is not null) {
                await requestInfo.Content.DisposeAsync().ConfigureAwait(false);
            }

            var responseHandler = GetResponseHandler(requestInfo);
            if (responseHandler == null) {
                try {
                    await this.ThrowIfFailedResponseAsync(response, errorMapping, span, cancellationToken).ConfigureAwait(false);
                    if (shouldReturnNull(response)) {
                        return default;
                    }

                    var (rootNode, bufferstream, jsonDocument) = await this.GetRootParseNodeQAsync(response, cancellationToken).ConfigureAwait(false);
                    using (bufferstream) {
                        using (jsonDocument) {
                            using var spanForDeserialization = this._ActivitySource?.StartActivity(nameof(IParseNode.GetCollectionOfPrimitiveValues));
                            var result = rootNode?.GetCollectionOfPrimitiveValues<ModelType>();
                            SetResponseType(result, span);
                            return result;
                        }
                    }
                } finally {
                    await DrainAsync(response, cancellationToken).ConfigureAwait(false);
                }
            } else {
                span?.AddEvent(new ActivityEvent(EventResponseHandlerInvokedKey));
                return await responseHandler.HandleResponseAsync<HttpResponseMessage, IEnumerable<ModelType>>(response, errorMapping).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// The key for the tracing event raised when a response handler is called.
        /// </summary>
        public const string EventResponseHandlerInvokedKey = "com.microsoft.kiota.response_handler_invoked";

        /// <summary>
        /// Send a <see cref="RequestInformation"/> instance with an instance of <typeparam name="ModelType"></typeparam>
        /// </summary>
        /// <param name="requestInfo">The <see cref="RequestInformation"/> instance to send</param>
        /// <param name="factory">The factory of the response model to deserialize the response into.</param>
        /// <param name="errorMapping">The error factories mapping to use in case of a failed request.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to use for cancelling the request.</param>
        /// <returns>The deserialized response model.</returns>
        public async Task<ModelType?> SendAsync<ModelType>(RequestInformation requestInfo, ParsableFactory<ModelType> factory, Dictionary<string, ParsableFactory<IParsable>>? errorMapping = default, CancellationToken cancellationToken = default) where ModelType : IParsable {
            using var span = this.StartTracingSpan(requestInfo, nameof(SendAsync));
            var response = await this.GetHttpResponseMessageAsync(requestInfo, cancellationToken, span).ConfigureAwait(false);
            if (requestInfo.Content is not null) {
                await requestInfo.Content.DisposeAsync().ConfigureAwait(false);
            }

            var responseHandler = GetResponseHandler(requestInfo);
            if (responseHandler == null) {
                try {
                    await this.ThrowIfFailedResponseAsync(response, errorMapping, span, cancellationToken).ConfigureAwait(false);
                    if (shouldReturnNull(response)) {
                        return default;
                    }

                    var (rootNode, bufferstream, jsonDocument) = await this.GetRootParseNodeQAsync(response, cancellationToken).ConfigureAwait(false);
                    using (bufferstream) {
                        using (jsonDocument) {
                            if (rootNode == null) {
                                return default;
                            }

                            using var spanForDeserialization = this._ActivitySource?.StartActivity(nameof(IParseNode.GetObjectValue));
                            var result = rootNode.GetObjectValue<ModelType>(factory);
                            SetResponseType(result, span);
                            return result;
                        }
                    }
                } finally {
                    if (typeof(ModelType) != typeof(Stream)) {
                        await DrainAsync(response, cancellationToken).ConfigureAwait(false);
                    }
                }
            } else {
                span?.AddEvent(new ActivityEvent(EventResponseHandlerInvokedKey));
                return await responseHandler.HandleResponseAsync<HttpResponseMessage, ModelType>(response, errorMapping).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Send a <see cref="RequestInformation"/> instance with a primitive instance of <typeparam name="ModelType"></typeparam>
        /// </summary>
        /// <param name="requestInfo">The <see cref="RequestInformation"/> instance to send</param>
        /// <param name="errorMapping">The error factories mapping to use in case of a failed request.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to use for cancelling the request.</param>
        /// <returns>The deserialized primitive response model.</returns>
        public async Task<ModelType?> SendPrimitiveAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] ModelType>(RequestInformation requestInfo, Dictionary<string, ParsableFactory<IParsable>>? errorMapping = default, CancellationToken cancellationToken = default) {
            using var span = this.StartTracingSpan(requestInfo, nameof(SendPrimitiveAsync));
            var modelType = typeof(ModelType);
            var isStreamResponse = modelType == typeof(Stream);
            var response = await this.GetHttpResponseMessageAsync(requestInfo, cancellationToken, span, isStreamResponse: isStreamResponse).ConfigureAwait(false);
            if (requestInfo.Content is not null) {
                await requestInfo.Content.DisposeAsync().ConfigureAwait(false);
            }
            var responseHandler = GetResponseHandler(requestInfo);
            if (responseHandler == null) {
                try {
                    await this.ThrowIfFailedResponseAsync(response, errorMapping, span, cancellationToken).ConfigureAwait(false);
                    if (shouldReturnNull(response)) {
                        return default;
                    }

                    if (isStreamResponse) {
                        var result = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                        if (result.CanSeek && result.Length == 0) {
                            await result.DisposeAsync().ConfigureAwait(false);
                            return default;
                        }
                        SetResponseType(result, span);
                        return (ModelType)(result as object);
                    } else {
                        var (rootNode, bufferstream, jsonDocument) = await this.GetRootParseNodeQAsync(response, cancellationToken).ConfigureAwait(false);
                        using (bufferstream) {
                            using (jsonDocument) {
                                object? result;
                                using var spanForDeserialization = this._ActivitySource?.StartActivity($"Get{modelType.Name.TrimEnd('?')}Value");
                                if (rootNode == null) {
                                    result = null;
                                } else if (modelType == typeof(bool?)) {
                                    result = rootNode.GetBoolValue();
                                } else if (modelType == typeof(byte?)) {
                                    result = rootNode.GetByteValue();
                                } else if (modelType == typeof(sbyte?)) {
                                    result = rootNode.GetSbyteValue();
                                } else if (modelType == typeof(string)) {
                                    result = rootNode.GetStringValue();
                                } else if (modelType == typeof(int?)) {
                                    result = rootNode.GetIntValue();
                                } else if (modelType == typeof(float?)) {
                                    result = rootNode.GetFloatValue();
                                } else if (modelType == typeof(long?)) {
                                    result = rootNode.GetLongValue();
                                } else if (modelType == typeof(double?)) {
                                    result = rootNode.GetDoubleValue();
                                } else if (modelType == typeof(decimal?)) {
                                    result = rootNode.GetDecimalValue();
                                } else if (modelType == typeof(Guid?)) {
                                    result = rootNode.GetGuidValue();
                                } else if (modelType == typeof(DateTime?)) {
                                    result = rootNode.GetDateTimeValue();
                                } else if (modelType == typeof(DateTimeOffset?)) {
                                    result = rootNode.GetDateTimeOffsetValue();
                                } else if (modelType == typeof(TimeSpan?)) {
                                    result = rootNode.GetTimeSpanValue();
                                } else if (modelType == typeof(DateOnly?)) {
                                    result = rootNode.GetDateValue();
                                } else if (modelType == typeof(TimeOnly?)) {
                                    result = rootNode.GetTimeValue();
                                } else if (rootNode.GetStringValue() is { Length: > 0 } rawValue) {
                                    result = EnumHelpers.GetEnumValue(modelType, rawValue);
                                } else {
                                    throw new InvalidOperationException("error handling the response, unexpected type");
                                }
                                SetResponseType(result, span);
                                return (ModelType)result!;
                            }
                        }
                    }
                } finally {
                    if (typeof(ModelType) != typeof(Stream)) {
                        await DrainAsync(response, cancellationToken).ConfigureAwait(false);
                    }
                }
            } else {
                span?.AddEvent(new ActivityEvent(EventResponseHandlerInvokedKey));
                return await responseHandler.HandleResponseAsync<HttpResponseMessage, ModelType>(response, errorMapping).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Send a <see cref="RequestInformation"/> instance with an empty request body
        /// </summary>
        /// <param name="requestInfo">The <see cref="RequestInformation"/> instance to send</param>
        /// <param name="errorMapping">The error factories mapping to use in case of a failed request.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to use for cancelling the request.</param>
        /// <returns></returns>
        public async Task SendNoContentAsync(RequestInformation requestInfo, Dictionary<string, ParsableFactory<IParsable>>? errorMapping = default, CancellationToken cancellationToken = default) {
            using var span = this.StartTracingSpan(requestInfo, nameof(SendNoContentAsync));
            var response = await this.GetHttpResponseMessageAsync(requestInfo, cancellationToken, span).ConfigureAwait(false);
            if (requestInfo.Content is not null) {
                await requestInfo.Content.DisposeAsync().ConfigureAwait(false);
            }
            var responseHandler = GetResponseHandler(requestInfo);
            if (responseHandler == null) {
                try {
                    await this.ThrowIfFailedResponseAsync(response, errorMapping, span, cancellationToken).ConfigureAwait(false);
                } finally {
                    await DrainAsync(response, cancellationToken).ConfigureAwait(false);
                }
            } else {
                span?.AddEvent(new ActivityEvent(EventResponseHandlerInvokedKey));
                await responseHandler.HandleResponseAsync<HttpResponseMessage, object>(response, errorMapping).ConfigureAwait(false);
            }
        }
        private static void SetResponseType(object? result, Activity? activity) {
            if (result != null) {
                _ = (activity?.SetTag("com.microsoft.kiota.response.type", result.GetType().FullName));
            }
        }
        private static async Task DrainAsync(HttpResponseMessage response, CancellationToken cancellationToken) {
            if (response.Content != null) {
                using var discard = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                response.Content.Dispose();
            }
            response.Dispose();
        }
        private static bool shouldReturnNull(HttpResponseMessage response) {
            return response.StatusCode == HttpStatusCode.NoContent
                   || response.Content == null
                   || response.Content.GetType().Name.Equals("EmptyContent", StringComparison.OrdinalIgnoreCase);
            // In NET 5 and above, Content is never null but represented by the internal class EmptyContent
            // which MAY return instances of EmptyReadStream on reading(which is not seekable thus we can't read/get the length)
            // https://github.com/dotnet/runtime/blob/main/src/libraries/System.Net.Http/src/System/Net/Http/EmptyReadStream.cs
        }

        /// <summary>
        /// The attribute name used to indicate whether an error code mapping was found.
        /// </summary>
        public const string ErrorMappingFoundAttributeName = "com.microsoft.kiota.error.mapping_found";

        /// <summary>
        /// The attribute name used to indicate whether the error response contained a body.
        /// </summary>
        public const string ErrorBodyFoundAttributeName = "com.microsoft.kiota.error.body_found";

        private async Task ThrowIfFailedResponseAsync(HttpResponseMessage response, Dictionary<string, ParsableFactory<IParsable>>? errorMapping, Activity? activityForAttributes, CancellationToken cancellationToken) {
            using var span = this._ActivitySource?.StartActivity(nameof(ThrowIfFailedResponseAsync));
            if (response.IsSuccessStatusCode || response.StatusCode is HttpStatusCode.Moved or HttpStatusCode.MovedPermanently or HttpStatusCode.Found or HttpStatusCode.Redirect) {
                return;
            }

            activityForAttributes?.SetStatus(ActivityStatusCode.Error, "received_error_response");

            var statusCodeAsInt = (int)response.StatusCode;
            var statusCodeAsString = statusCodeAsInt.ToString();
            var responseHeadersDictionary = new Dictionary<string, IEnumerable<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var header in response.Headers) {
                responseHeadersDictionary[header.Key] = header.Value;
            }

            ParsableFactory<IParsable>? errorFactory;
            if ((errorMapping == null)
                || (!errorMapping.TryGetValue(statusCodeAsString, out errorFactory)
                    && !(statusCodeAsInt >= 400 && statusCodeAsInt < 500 && errorMapping.TryGetValue("4XX", out errorFactory))
                    && !(statusCodeAsInt >= 500 && statusCodeAsInt < 600 && errorMapping.TryGetValue("5XX", out errorFactory))
                    && !errorMapping.TryGetValue("XXX", out errorFactory)
                )) {
                activityForAttributes?.SetTag(ErrorMappingFoundAttributeName, false);
                throw new ApiException($"The server returned an unexpected status code and no error factory is registered for this code: {statusCodeAsString}") {
                    ResponseStatusCode = statusCodeAsInt,
                    ResponseHeaders = responseHeadersDictionary
                };
            }
            activityForAttributes?.SetTag(ErrorMappingFoundAttributeName, true);

            var (rootNode, bufferstream, jsonDocument) = await this.GetRootParseNodeQAsync(response, cancellationToken).ConfigureAwait(false);
            using (bufferstream) {
                using (jsonDocument) {
                    activityForAttributes?.SetTag(ErrorBodyFoundAttributeName, rootNode != null);
                    var spanForDeserialization = this._ActivitySource?.StartActivity(nameof(IParseNode.GetObjectValue));
                    var result = rootNode?.GetObjectValue(errorFactory);
                    SetResponseType(result, activityForAttributes);
                    spanForDeserialization?.Dispose();
                    if (result is not Exception ex) {
                        throw new ApiException($"The server returned an unexpected status code and the error registered for this code failed to deserialize: {statusCodeAsString}") {
                            ResponseStatusCode = statusCodeAsInt,
                            ResponseHeaders = responseHeadersDictionary
                        };
                    }

                    if (result is ApiException apiEx) {
                        apiEx.ResponseStatusCode = statusCodeAsInt;
                        apiEx.ResponseHeaders = responseHeadersDictionary;
                    }

                    throw ex;
                }
            }
        }
        private static IResponseHandler? GetResponseHandler(RequestInformation requestInfo) {
            return requestInfo.GetRequestOption<ResponseHandlerOption>()?.ResponseHandler;
        }

        private async Task<GetRootParseNodeResultQ> GetRootParseNodeQAsync(HttpResponseMessage response, CancellationToken cancellationToken) {
            using var span = this._ActivitySource?.StartActivity(nameof(GetRootParseNodeQAsync));
            var responseContentType = response.Content?.Headers?.ContentType?.MediaType?.ToLowerInvariant();
            if (string.IsNullOrEmpty(responseContentType)) {
                return new GetRootParseNodeResultQ(default, default, default);
            }
            var contentStream = await (response.Content?.ReadAsStreamAsync(cancellationToken) ?? Task.FromResult(Stream.Null)).ConfigureAwait(false);
            if (contentStream == Stream.Null || (contentStream.CanSeek && contentStream.Length == 0)) {
                return new GetRootParseNodeResultQ(default, default, default); // ensure a useful stream is passed to the factory
            }
            var (rootNode, bufferStream, jsonDocument) = await this._PNodeFactory.GetRootParseNodeAsync(responseContentType!, contentStream, cancellationToken).ConfigureAwait(false);
            return new(rootNode, contentStream, jsonDocument);
        }

        private const string _ClaimsKey = "claims";
        private const string _BearerAuthenticationScheme = "Bearer";

        private async Task<HttpResponseMessage> GetHttpResponseMessageAsync(RequestInformation requestInfo, CancellationToken cancellationToken, Activity? activityForAttributes, string? claims = default, bool isStreamResponse = false) {
            using var span = this._ActivitySource?.StartActivity(nameof(GetHttpResponseMessageAsync));
            if (requestInfo == null) {
                throw new ArgumentNullException(nameof(requestInfo));
            }

            this.SetBaseUrlForRequestInformation(requestInfo);

            var additionalAuthenticationContext = string.IsNullOrEmpty(claims) ? null : new Dictionary<string, object> { { _ClaimsKey, claims! } };
            await this._AuthProvider.AuthenticateRequestAsync(requestInfo, additionalAuthenticationContext, cancellationToken).ConfigureAwait(false);

            using var message = this.GetRequestMessageFromRequestInformation(requestInfo, activityForAttributes);
            HttpResponseMessage response;
            if (isStreamResponse) {
                response = await this._Client.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            } else {
                response = await this._Client.SendAsync(message, cancellationToken).ConfigureAwait(false);
            }
            if (response == null) {
                var ex = new InvalidOperationException("Could not get a response after calling the service");
                throw ex;
            }
            if (response.Headers.TryGetValues("Content-Length", out var contentLengthValues)) {
                using var contentLengthEnumerator = contentLengthValues.GetEnumerator();
                if (contentLengthEnumerator.MoveNext() && int.TryParse(contentLengthEnumerator.Current, out var contentLength)) {
                    _ = (activityForAttributes?.SetTag("http.response.body.size", contentLength));
                }
            }
            if (response.Headers.TryGetValues("Content-Type", out var contentTypeValues)) {
                using var contentTypeEnumerator = contentTypeValues.GetEnumerator();
                if (contentTypeEnumerator.MoveNext()) {
                    _ = (activityForAttributes?.SetTag("http.response.header.content-type", contentTypeEnumerator.Current));
                }
            }
            _ = (activityForAttributes?.SetTag("http.response.status_code", (int)response.StatusCode));
            _ = (activityForAttributes?.SetTag("network.protocol.name", $"{response.Version.Major}.{response.Version.Minor}"));

            return await this.RetryCAEResponseIfRequiredAsync(response, requestInfo, cancellationToken, claims, activityForAttributes).ConfigureAwait(false);
        }


        /// <summary>
        /// The key for the event raised by tracing when an authentication challenge is received
        /// </summary>
        public const string AuthenticateChallengedEventKey = "com.microsoft.kiota.authenticate_challenge_received";

        internal const string RetryCountAttributeName = "http.request.resend_count";

        private async Task<HttpResponseMessage> RetryCAEResponseIfRequiredAsync(HttpResponseMessage response, RequestInformation requestInfo, CancellationToken cancellationToken, string? claims, Activity? activityForAttributes) {
            using var span = this._ActivitySource?.StartActivity(nameof(RetryCAEResponseIfRequiredAsync));
            if (response.StatusCode == HttpStatusCode.Unauthorized &&
                string.IsNullOrEmpty(claims) && // avoid infinite loop, we only retry once
                (requestInfo.Content?.CanSeek ?? true)) {
                var responseClaims = ContinuousAccessEvaluation.GetClaims(response);
                if (string.IsNullOrEmpty(responseClaims)) {
                    return response;
                }
                _ = (span?.AddEvent(new ActivityEvent(AuthenticateChallengedEventKey)));
                _ = (activityForAttributes?.SetTag(RetryCountAttributeName, 1));
                _ = (requestInfo.Content?.Seek(0, SeekOrigin.Begin));
                await DrainAsync(response, cancellationToken).ConfigureAwait(false);
                return await this.GetHttpResponseMessageAsync(requestInfo, cancellationToken, activityForAttributes, responseClaims).ConfigureAwait(false);
            }
            return response;
        }

        private void SetBaseUrlForRequestInformation(RequestInformation requestInfo) {
            IDictionaryExtensions.AddOrReplace(requestInfo.PathParameters, "baseurl", this.BaseUrl!);
        }
        /// <inheritdoc/>
        public async Task<T?> ConvertToNativeRequestAsync<T>(RequestInformation requestInfo, CancellationToken cancellationToken = default) {
            await this._AuthProvider.AuthenticateRequestAsync(requestInfo, null, cancellationToken).ConfigureAwait(false);
            if (this.GetRequestMessageFromRequestInformation(requestInfo, null) is T result) {
                return result;
            } else {
                throw new InvalidOperationException($"Could not convert the request information to a {typeof(T).Name}");
            }
        }

        private HttpRequestMessage GetRequestMessageFromRequestInformation(RequestInformation requestInfo, Activity? activityForAttributes) {
            using var span = this._ActivitySource?.StartActivity(nameof(GetRequestMessageFromRequestInformation));
            this.SetBaseUrlForRequestInformation(requestInfo);// this method can also be called from a different context so ensure the baseUrl is added.
            activityForAttributes?.SetTag("http.request.method", requestInfo.HttpMethod.ToString());
            var requestUri = requestInfo.URI;
            activityForAttributes?.SetTag("url.scheme", requestUri.Scheme);
            activityForAttributes?.SetTag("server.address", requestUri.Host);
            if (this._ObsOptions.IncludeEUIIAttributes) {
                activityForAttributes?.SetTag("url.full", requestUri.ToString());
            }

            var message = new HttpRequestMessage {
                Method = new HttpMethod(requestInfo.HttpMethod.ToString().ToUpperInvariant()),
                RequestUri = requestUri,
                Version = this._HttpVersion ?? this._Client.DefaultRequestVersion,
                VersionPolicy = this._Client.DefaultVersionPolicy
            };

            if (requestInfo.RequestOptions != null) {
                foreach (var option in requestInfo.RequestOptions) {
                    message.Options.Set(new HttpRequestOptionsKey<IRequestOption>(option.GetType().FullName!), option);
                }
            }
            message.Options.Set(new HttpRequestOptionsKey<IRequestOption>(typeof(ObservabilityOptions).FullName!), this._ObsOptions);

            if (requestInfo.Content != null && requestInfo.Content != Stream.Null) {
                message.Content = new StreamContent(requestInfo.Content);
            }

            if (requestInfo.Headers != null) {
                foreach (var header in requestInfo.Headers) {
                    if (!message.Headers.TryAddWithoutValidation(header.Key, header.Value) && message.Content != null) {
                        message.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);// Try to add the headers we couldn't add to the HttpRequestMessage before to the HttpContent
                    }
                }
            }

            if (message.Content != null) {
                if (message.Content.Headers.TryGetValues("Content-Length", out var contentLenValues)) {
                    var contentLenEnumerator = contentLenValues.GetEnumerator();
                    if (contentLenEnumerator.MoveNext() && int.TryParse(contentLenEnumerator.Current, out var contentLenValueInt)) {
                        activityForAttributes?.SetTag("http.request.body.size", contentLenValueInt);
                    }
                }
                if (message.Content.Headers.TryGetValues("Content-Type", out var contentTypeValues)) {
                    var contentTypeEnumerator = contentTypeValues.GetEnumerator();
                    if (contentTypeEnumerator.MoveNext()) {
                        activityForAttributes?.SetTag("http.request.header.content-type", contentTypeEnumerator.Current);
                    }
                }
            }
            return message;
        }

        /// <summary>
        /// Enable the backing store with the provided <see cref="IBackingStoreFactory"/>
        /// </summary>
        /// <param name="backingStoreFactory">The <see cref="IBackingStoreFactory"/> to use</param>
        public void EnableBackingStore(IBackingStoreFactory backingStoreFactory) {
            this._PNodeFactory = ApiClientBuilder.EnableBackingStoreForParseNodeFactory(this._PNodeFactory) ?? throw new InvalidOperationException("Could not enable backing store for the parse node factory");
            this._SWriterFactory = ApiClientBuilder.EnableBackingStoreForSerializationWriterFactory(this._SWriterFactory) ?? throw new InvalidOperationException("Could not enable backing store for the serializer writer factory");
            if (backingStoreFactory != null) {
                BackingStoreFactorySingleton.Instance = backingStoreFactory;
            }
        }

        /// <summary>
        /// Dispose/cleanup the client
        /// </summary>
        public void Dispose() {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose/cleanup the client
        /// </summary>
        protected virtual void Dispose(bool disposing) {
            // Cleanup
            if (this._CreatedClient) {
                this._Client?.Dispose();
            }
        }
    }
}
