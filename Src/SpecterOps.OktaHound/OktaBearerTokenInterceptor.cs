using System.Net.Http.Headers;
using RestSharp.Interceptors;

namespace SpecterOps.OktaHound;

/// <summary>
/// Injects a fresh OAuth 2.0 bearer token into every outgoing Okta API request
/// when client secret authentication is used.
/// </summary>
/// <remarks>
/// Attaching the token per request (instead of setting Configuration.AccessToken once)
/// keeps long-running enumerations working after the initial access token expires.
/// </remarks>
internal sealed class OktaBearerTokenInterceptor : Interceptor
{
    private readonly OktaClientSecretTokenProvider _tokenProvider;

    public OktaBearerTokenInterceptor(OktaClientSecretTokenProvider tokenProvider)
    {
        _tokenProvider = tokenProvider;
    }

    public override async ValueTask BeforeHttpRequest(HttpRequestMessage requestMessage, CancellationToken cancellationToken)
    {
        string accessToken = await _tokenProvider.GetAccessTokenAsync(cancellationToken).ConfigureAwait(false);
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }
}
