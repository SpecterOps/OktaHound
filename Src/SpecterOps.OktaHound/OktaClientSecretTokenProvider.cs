using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Okta.Sdk.Abstractions.Configuration.Providers.Yaml;
using Okta.Sdk.Client;

namespace SpecterOps.OktaHound;

/// <summary>
/// Acquires and caches OAuth 2.0 access tokens using the client credentials flow with a client secret.
/// </summary>
/// <remarks>
/// The Okta SDK only supports the private key JWT client authentication method,
/// while Okta API service integrations are limited to client secret authentication.
/// See https://github.com/okta/okta-sdk-dotnet/issues/817 for details.
/// </remarks>
internal sealed class OktaClientSecretTokenProvider : IDisposable
{
    /// <summary>
    /// Relative path of the Okta org authorization server token endpoint.
    /// </summary>
    private const string TokenEndpointPath = "/oauth2/v1/token";

    /// <summary>
    /// Access tokens are renewed this long before they actually expire,
    /// so that requests already in flight never carry a token that expires mid-request.
    /// </summary>
    private static readonly TimeSpan ExpirationBuffer = TimeSpan.FromMinutes(5);

    private readonly Configuration _configuration;
    private readonly string _clientSecret;
    private readonly ILogger _logger;
    private readonly SemaphoreSlim _renewalLock = new(1, 1);
    private readonly HttpClient _httpClient;

    private string? _accessToken;
    private DateTimeOffset _accessTokenExpiresAt = DateTimeOffset.MinValue;

    public OktaClientSecretTokenProvider(Configuration configuration, string clientSecret, ILogger logger)
    {
        _configuration = configuration;
        _clientSecret = clientSecret;
        _logger = logger;

        HttpClientHandler handler = new();

        if (configuration.UseProxy == true && configuration.Proxy != null)
        {
            handler.Proxy = ProxyConfiguration.GetProxy(configuration.Proxy);
            handler.UseProxy = true;
        }

        _httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromMilliseconds(configuration.ConnectionTimeout ?? Configuration.DefaultConnectionTimeout)
        };
    }

    /// <summary>
    /// Returns a cached access token, or requests a new one if the cached token is missing or about to expire.
    /// </summary>
    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        // Fast path without locking; the cached token stays valid for the entire buffer window.
        string? accessToken = _accessToken;

        if (accessToken != null && DateTimeOffset.UtcNow < _accessTokenExpiresAt - ExpirationBuffer)
        {
            return accessToken;
        }

        await _renewalLock.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            // Re-check after acquiring the lock, as another caller may have renewed the token in the meantime.
            if (_accessToken == null || DateTimeOffset.UtcNow >= _accessTokenExpiresAt - ExpirationBuffer)
            {
                (_accessToken, _accessTokenExpiresAt) = await RequestAccessTokenAsync(cancellationToken).ConfigureAwait(false);
            }

            return _accessToken;
        }
        finally
        {
            _renewalLock.Release();
        }
    }

    /// <summary>
    /// Resolves the client secret from the command line or a configuration file, in that order.
    /// </summary>
    /// <param name="fromCommandLine">The client secret provided on the command line, if any.</param>
    /// <param name="configFilePath">Path to a caller-supplied YAML or JSON configuration file that takes precedence over the default okta.yaml locations, or null.</param>
    /// <returns>The client secret, or null if none is configured.</returns>
    public static string? ResolveClientSecret(string? fromCommandLine, string? configFilePath = null)
    {
        if (!string.IsNullOrEmpty(fromCommandLine))
        {
            return fromCommandLine;
        }

        // Search the same configuration file locations as Configuration.GetConfigurationOrDefault.
        // The Okta SDK Configuration class has no ClientSecret property,
        // so the value must be read from the configuration files directly.
        string homeOktaYamlLocation = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".okta", "okta.yaml");
        string applicationOktaYamlLocation = Path.Combine(Directory.GetCurrentDirectory(), "okta.yaml");

        ConfigurationBuilder configBuilder = new();
        configBuilder
            .AddYamlFile(homeOktaYamlLocation, optional: true)
            .AddYamlFile(applicationOktaYamlLocation, optional: true);

        if (configFilePath != null)
        {
            // A caller-supplied configuration file outranks the default locations.
            if (Path.GetExtension(configFilePath).Equals(".json", StringComparison.OrdinalIgnoreCase))
            {
                configBuilder.AddJsonFile(configFilePath, optional: false);
            }
            else
            {
                configBuilder.AddYamlFile(configFilePath, optional: false);
            }
        }

        return configBuilder.Build()["okta:client:clientSecret"];
    }

    private async Task<(string accessToken, DateTimeOffset expiresAt)> RequestAccessTokenAsync(CancellationToken cancellationToken)
    {
        Uri tokenEndpoint = new(new Uri(_configuration.OktaDomain), TokenEndpointPath);
        _logger.LogDebug("Requesting an OAuth 2.0 access token from {TokenEndpoint}...", tokenEndpoint);

        using HttpRequestMessage request = new(System.Net.Http.HttpMethod.Post, tokenEndpoint);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // The client_secret_basic authentication method requires the credentials
        // to be form-urlencoded before they are Base64-encoded (RFC 6749 section 2.3.1).
        string credentials = $"{Uri.EscapeDataString(_configuration.ClientId)}:{Uri.EscapeDataString(_clientSecret)}";
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials)));

        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["scope"] = string.Join(' ', _configuration.Scopes)
        });

        using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        string payload = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            string message = TryGetErrorDescription(payload) ?? payload;

            // Log the root cause here, as the exception may get swallowed by the HTTP client pipeline.
            _logger.LogError(
                "Failed to obtain an OAuth 2.0 access token: {StatusCode} {Message}",
                (int)response.StatusCode,
                message);

            throw new InvalidOperationException($"Failed to obtain an OAuth 2.0 access token: {message}");
        }

        using JsonDocument json = JsonDocument.Parse(payload);
        string? accessToken = json.RootElement.GetProperty("access_token").GetString();
        int expiresIn = json.RootElement.GetProperty("expires_in").GetInt32();

        if (string.IsNullOrEmpty(accessToken))
        {
            throw new InvalidOperationException("The Okta token endpoint returned an empty access token.");
        }

        _logger.LogDebug("Obtained an OAuth 2.0 access token that expires in {ExpiresIn} seconds.", expiresIn);

        return (accessToken, DateTimeOffset.UtcNow.AddSeconds(expiresIn));
    }

    /// <summary>
    /// Extracts a human-readable error message from an Okta token endpoint error response.
    /// </summary>
    private static string? TryGetErrorDescription(string payload)
    {
        try
        {
            using JsonDocument json = JsonDocument.Parse(payload);

            // OAuth 2.0 style error response
            if (json.RootElement.TryGetProperty("error", out JsonElement error))
            {
                string? description = json.RootElement.TryGetProperty("error_description", out JsonElement errorDescription)
                    ? errorDescription.GetString()
                    : null;

                return description != null ? $"{error.GetString()}: {description}" : error.GetString();
            }

            // Okta API style error response
            if (json.RootElement.TryGetProperty("errorSummary", out JsonElement errorSummary))
            {
                return errorSummary.GetString();
            }
        }
        catch (JsonException)
        {
            // The response body is not valid JSON, so the caller falls back to the raw payload.
        }

        return null;
    }

    public void Dispose()
    {
        _httpClient.Dispose();
        _renewalLock.Dispose();
    }
}
