using System.Xml;
using Okta.Sdk.Model;

namespace SpecterOps.OktaHound.Model.Okta;

internal sealed class OktaApiToken : OktaNode
{
    public const string NodeKind = "Okta_ApiToken";
    public const string ApiTokenForEdgeKind = "Okta_ApiTokenFor";

    public OktaApiToken(ApiToken apiToken, string domainName) : base(apiToken.Id, domainName, NodeKind)
    {
        Name = apiToken.Name;
        DisplayName = apiToken.Name;

        SetProperty("userId", apiToken.UserId);
        SetProperty("clientName", apiToken.ClientName);
        SetProperty("created", apiToken.Created);
        SetProperty("lastUpdated", apiToken.LastUpdated);
        SetProperty("expiresAt", apiToken.ExpiresAt);
        SetProperty("networkConnection", apiToken.Network?.Connection);

        if (!string.IsNullOrEmpty(apiToken.TokenWindow))
        {
            // Parse ISO-8601 period, e.g., "P30D".
            var tokenWindow = XmlConvert.ToTimeSpan(apiToken.TokenWindow);
            SetProperty("tokenWindow", tokenWindow);
        }
    }
}
