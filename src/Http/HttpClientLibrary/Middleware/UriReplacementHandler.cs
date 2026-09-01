using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using Brimborium.Kiota.Http.HttpClientLibrary.Extensions;
using Brimborium.Kiota.Http.HttpClientLibrary.Middleware.Options;

namespace Brimborium.Kiota.Http.HttpClientLibrary.Middleware;

/// <summary>
/// Replaces a portion of the URL.
/// </summary>
/// <typeparam name="TUriReplacementHandlerOption">A type with the rules used to perform a URI replacement.</typeparam>
public class UriReplacementHandler<TUriReplacementHandlerOption> : DelegatingHandler where TUriReplacementHandlerOption : IUriReplacementHandlerOption
{
    private readonly TUriReplacementHandlerOption? _uriReplacement;

    /// <summary>
    /// Creates a new UriReplacementHandler.
    /// </summary>
    /// <param name="uriReplacement">An object with the URI replacement rules.</param>
    public UriReplacementHandler(TUriReplacementHandlerOption? uriReplacement = default)
    {
        this._uriReplacement = uriReplacement;
    }

    /// <inheritdoc/>
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
    {
        var uriReplacement = request.GetRequestOption<TUriReplacementHandlerOption>() ?? _uriReplacement;

        // If there is no URI replacement to apply, then just skip this handler.
        if(uriReplacement is null)
        {
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }

        Activity? activity;
        if(request.GetRequestOption<ObservabilityOptions>() is { } obsOptions)
        {
            var activitySource = ActivitySourceRegistry.DefaultInstance.GetOrCreateActivitySource(obsOptions.TracerInstrumentationName);
            activity = activitySource.StartActivity($"{nameof(UriReplacementHandler<TUriReplacementHandlerOption>)}_{nameof(SendAsync)}");
            _ = (activity?.SetTag("com.Brimborium.Kiota.handler.uri_replacement.enable", uriReplacement.IsEnabled()));
        }
        else
        {
            activity = null;
        }

        try
        {
            request.RequestUri = uriReplacement.Replace(request.RequestUri);
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            activity?.Dispose();
        }
    }
}
