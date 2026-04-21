using System.Text.Json.Serialization;
using Okta.Sdk.Model;

namespace SpecterOps.OktaHound.Model.Okta;

internal sealed class OktaApiServiceIntegration : OktaNode
{
    public const string CreatorOfEdgeKind = "Okta_CreatorOf";
    private const string NodeKind = "Okta_ApiServiceIntegration";
    private const string IntegrationTypePropertyName = "appType";
    private const string PermissionsPropertyName = "oauthScopes";

    [JsonIgnore]
    public string? IntegrationType => GetProperty<string>(IntegrationTypePropertyName);

    [JsonIgnore]
    public List<string>? Permissions => GetProperty<List<string>>(PermissionsPropertyName);

    public OktaApiServiceIntegration(APIServiceIntegrationInstance service, OktaOrganization organization) : base(service.Id, organization, NodeKind)
    {
        Name = service.Name;
        DisplayName = service.Name;

        SetProperty(PermissionsPropertyName, service.GrantedScopes);
        SetProperty("createdAt", service.CreatedAt);
        SetProperty(IntegrationTypePropertyName, service.Type);
    }
}
