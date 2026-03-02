using System.Text.Json.Serialization;
using Okta.Sdk.Model;

namespace SpecterOps.OktaHound.Database;

public sealed class OktaApplicationGrant
{
    public string Id { get; set; } = string.Empty;

    public string ApplicationId { get; set; } = string.Empty;

    public string? ClientId { get; set; }

    public string? Issuer { get; set; }

    public string ScopeId { get; set; } = string.Empty;

    public string? Source { get; set; }

    public string? Status { get; set; }

    public string? UserId { get; set; }

    public string? CreatedById { get; set; }

    public string? CreatedByType { get; set; }

    public DateTimeOffset? Created { get; set; }

    public DateTimeOffset? LastUpdated { get; set; }

    [JsonIgnore]
    public OktaApplication? Application { get; set; } = null!;

    private OktaApplicationGrant()
    {
    }

    public OktaApplicationGrant(OAuth2ScopeConsentGrant grant, string applicationId)
    {
        Id = grant.Id;
        ApplicationId = applicationId;
        ClientId = grant.ClientId;
        Created = grant.Created;
        CreatedById = grant.CreatedBy?.Id;
        CreatedByType = grant.CreatedBy?.Type;
        Issuer = grant.Issuer;
        LastUpdated = grant.LastUpdated;
        ScopeId = grant.ScopeId;
        Source = grant.Source?.Value;
        Status = grant.Status?.Value;
        UserId = grant.UserId;
    }
}
