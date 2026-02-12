using Okta.Sdk.Model;
using SpecterOps.OktaHound.Model.OpenGraph;

namespace SpecterOps.OktaHound.Model.Okta;

internal sealed class OktaAuthorizationServer : OktaNode
{
    private const string NodeKind = "Okta_AuthorizationServer";

    public OktaAuthorizationServer(AuthorizationServer server, string domainName) : base(server.Id, domainName, NodeKind)
    {
        Name = server.Name;
        DisplayName = server.Name;

        SetProperty("created", server.Created);
        SetProperty("lastUpdated", server.LastUpdated);
        SetProperty("description", server.Description);
        SetProperty("status", server.Status?.Value);
        SetProperty("issuer", server.Issuer);
        SetProperty("issuerMode", server.IssuerMode);
        SetProperty("audiences", server.Audiences);
        // TODO: Parse server.Credentials
    }

    public static new OpenGraphEdgeNode CreateEdgeNode(string id) => new(id, NodeKind);
}
