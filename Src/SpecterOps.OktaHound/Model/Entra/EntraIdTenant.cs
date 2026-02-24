using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using SpecterOps.OktaHound.Model.OpenGraph;

namespace SpecterOps.OktaHound.Model.Entra;

internal static class EntraIdTenant
{
    private const string NodeKind = "AZTenant";

    [return: NotNullIfNotNull(nameof(tenantId))]
    public static OpenGraphEdgeNode? CreateEdgeNode(string? tenantId)
    {
        if (tenantId is null)
        {
            return null;
        }

        return new OpenGraphEdgeNode(tenantId, NodeKind);
    }

    public static string? GetRegionFromEndpoint(string? endpoint)
    {
        if (string.IsNullOrEmpty(endpoint))
        {
            return null;
        }

        // TODO: Consider using an Enum for the Entra region
        if (endpoint.StartsWith("https://login.microsoftonline.com", StringComparison.OrdinalIgnoreCase))
        {
            return "Global";
        }
        else if (endpoint.StartsWith("https://login.microsoftonline.us", StringComparison.OrdinalIgnoreCase))
        {
            return "USGovernment";
        }
        else if (endpoint.StartsWith("https://login.partner.microsoftonline.cn", StringComparison.OrdinalIgnoreCase))
        {
            return "China";
        }
        else
        {
            // Unknown or unsupported endpoint format
            return null;
        }
    }

    public static string? ParseTenantIdFromUrl(string? uriString)
    {
        // URL format: https://login.microsoftonline.com/{tenantId}/saml2
        // or https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token
        // Sample: https://login.microsoftonline.com/996af01c-adef-43b6-af73-7ae92866441e/saml2
        if (string.IsNullOrEmpty(uriString))
        {
            return null;
        }

        Uri uri = new Uri(uriString);

        if (!uri.Host.Contains("microsoftonline", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }
        string[]? segments = uri.Segments;

        if (segments.Length >= 2 && segments[0] == "/")
        {
            return segments[1].TrimEnd('/'); // Remove trailing slash
        }

        return null;
    }

    public static async Task<string?> GetTenantIdFromOnMicrosoftDomain(string? onMicrosoftDomain)
    {
        // The onMicrosoftDomain is typically in the format "contoso" for the tenant "contoso.onmicrosoft.com".
        // We can reconstruct the tenant ID by appending ".onmicrosoft.com".
        if (string.IsNullOrWhiteSpace(onMicrosoftDomain))
        {
            return null;
        }

        // TODO: Support sovereign clouds
        string domainName = $"{onMicrosoftDomain}.onmicrosoft.com";
        string configUrl = $"https://login.microsoftonline.com/{domainName}/.well-known/openid-configuration";

        // Translate the domain to Tenant ID by querying the OpenID Connect configuration endpoint.
        try
        {
            using var httpClient = new HttpClient();
            var response = await httpClient.GetAsync(configUrl).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            using var jsonDoc = JsonDocument.Parse(content);

            if (jsonDoc.RootElement.TryGetProperty("token_endpoint", out var tokenEndpointElement))
            {
                // The token_endpoint typically contains the tenant ID in the URL.
                // Example: https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token
                string? tokenEndpoint = tokenEndpointElement.GetString();
                return ParseTenantIdFromUrl(tokenEndpoint);
            }
        }
        catch
        {
            // Ignore errors and return null (not found)
        }

        return null;
    }
}
