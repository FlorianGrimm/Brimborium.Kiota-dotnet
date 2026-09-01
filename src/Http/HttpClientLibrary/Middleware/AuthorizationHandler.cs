// ------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Brimborium.Kiota.Abstractions.Authentication;
using Brimborium.Kiota.Http.HttpClientLibrary.Extensions;

namespace Brimborium.Kiota.Http.HttpClientLibrary.Middleware
{
    /// <summary>
    /// Adds an Authorization header to the request if the header is not already present.
    /// Also handles Continuous Access Evaluation (CAE) claims challenges if the initial
    /// token request was made using this handler
    /// </summary>
    public class AuthorizationHandler : DelegatingHandler
    {

        private const string AuthorizationHeader = "Authorization";
        private readonly BaseBearerTokenAuthenticationProvider authenticationProvider;

        /// <summary>
        ///  Constructs an <see cref="AuthorizationHandler"/>
        /// </summary>
        /// <param name="authenticationProvider"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public AuthorizationHandler(BaseBearerTokenAuthenticationProvider authenticationProvider)
        {
            this.authenticationProvider = authenticationProvider ?? throw new ArgumentNullException(nameof(authenticationProvider));
        }

        /// <summary>
        ///     Adds an Authorization header if not already provided
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
                CancellationToken cancellationToken)
        {
            if(request == null) {
                throw new ArgumentNullException(nameof(request));
            }

            Activity? activity = null;
            if(request.GetRequestOption<ObservabilityOptions>() is { } obsOptions)
            {
                var activitySource = ActivitySourceRegistry.DefaultInstance.GetOrCreateActivitySource(obsOptions.TracerInstrumentationName);
                activity = activitySource?.StartActivity($"{nameof(AuthorizationHandler)}_{nameof(SendAsync)}");
                _ = (activity?.SetTag("com.Brimborium.Kiota.handler.authorization.enable", true));
            }
            try
            {
                if(request.Headers.Contains(AuthorizationHeader))
                {
                    _ = (activity?.SetTag("com.Brimborium.Kiota.handler.authorization.token_present", true));
                    return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
                }
                Dictionary<string, object> additionalAuthenticationContext = new Dictionary<string, object>();
                await AuthenticateRequestAsync(request, additionalAuthenticationContext, activity, cancellationToken).ConfigureAwait(false);
                var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
                if(response.StatusCode != HttpStatusCode.Unauthorized || response.RequestMessage == null || !response.RequestMessage.IsBuffered()) {
                    return response;
                }
                // Attempt CAE claims challenge
                var claims = ContinuousAccessEvaluation.GetClaims(response);
                if(string.IsNullOrEmpty(claims)) {
                    return response;
                }

                _ = (activity?.AddEvent(new ActivityEvent("com.Brimborium.Kiota.handler.authorization.challenge_received")));
                additionalAuthenticationContext[ContinuousAccessEvaluation.ClaimsKey] = claims;
                var retryRequest = await response.RequestMessage.CloneAsync(cancellationToken);
                await AuthenticateRequestAsync(retryRequest, additionalAuthenticationContext, activity, cancellationToken).ConfigureAwait(false);
                _ = (activity?.SetTag("http.request.resend_count", 1));
                return await base.SendAsync(retryRequest, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                activity?.Dispose();
            }
        }

        private async Task AuthenticateRequestAsync(HttpRequestMessage request,
                Dictionary<string, object> additionalAuthenticationContext,
                Activity? activityForAttributes,
                CancellationToken cancellationToken)
        {
            var accessTokenProvider = authenticationProvider.AccessTokenProvider;
            if(request.RequestUri == null || !accessTokenProvider.AllowedHostsValidator.IsUrlHostValid(
                    request.RequestUri))
            {
                return;
            }
            var accessToken = await accessTokenProvider.GetAuthorizationTokenAsync(
                    request.RequestUri,
                    additionalAuthenticationContext, cancellationToken).ConfigureAwait(false);
            _ = (activityForAttributes?.SetTag("com.Brimborium.Kiota.handler.authorization.token_obtained", true));
            if(string.IsNullOrEmpty(accessToken)) {
                return;
            }

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }
    }
}
