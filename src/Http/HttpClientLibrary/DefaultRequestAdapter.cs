// ------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------

using System.Net.Http;
using Brimborium.Kiota.Abstractions;
using Brimborium.Kiota.Abstractions.Authentication;
using Brimborium.Kiota.Abstractions.Serialization;
using Brimborium.Kiota.Http.HttpClientLibrary;
using Brimborium.Kiota.Serialization.Form;
using Brimborium.Kiota.Serialization.Json;
using Brimborium.Kiota.Serialization.Multipart;
using Brimborium.Kiota.Serialization.Text;

namespace Brimborium.Kiota.Bundle
{
    /// <summary>
    /// The <see cref="IRequestAdapter"/> implementation that derived from <see cref="HttpClientRequestAdapter"/> with registrations configured.
    /// </summary>
    public class DefaultRequestAdapter : HttpClientRequestAdapter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultRequestAdapter"/> class.
        /// </summary>
        /// <param name="authenticationProvider">The authentication provider.</param>
        /// <param name="parseNodeFactory">The parse node factory.</param>
        /// <param name="serializationWriterFactory">The serialization writer factory.</param>
        /// <param name="httpClient">The native HTTP client.</param>
        /// <param name="observabilityOptions">The observability options.</param>
        public DefaultRequestAdapter(IAuthenticationProvider authenticationProvider, IParseNodeFactory? parseNodeFactory = null, ISerializationWriterFactory? serializationWriterFactory = null, HttpClient? httpClient = null, ObservabilityOptions? observabilityOptions = null) : base(authenticationProvider, parseNodeFactory, serializationWriterFactory, httpClient, observabilityOptions)
        {
            SetupDefaults();
        }

        private static void SetupDefaults()
        {
            // Setup the default serializers/deserializers
            ApiClientBuilder.RegisterDefaultSerializer<JsonSerializationWriterFactory>();
            ApiClientBuilder.RegisterDefaultSerializer<TextSerializationWriterFactory>();
            ApiClientBuilder.RegisterDefaultSerializer<FormSerializationWriterFactory>();
            ApiClientBuilder.RegisterDefaultSerializer<MultipartSerializationWriterFactory>();
            ApiClientBuilder.RegisterDefaultDeserializer<JsonParseNodeFactory>();
            ApiClientBuilder.RegisterDefaultDeserializer<TextParseNodeFactory>();
            ApiClientBuilder.RegisterDefaultDeserializer<FormParseNodeFactory>();
        }
    }
}
