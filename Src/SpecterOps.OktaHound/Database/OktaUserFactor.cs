using System.Text.Json.Serialization;
using Okta.Sdk.Model;

namespace SpecterOps.OktaHound.Database;

public sealed class OktaUserFactor
{
    public string Id { get; set; } = string.Empty;

    public DateTimeOffset? Created { get; set; }
    public DateTimeOffset? LastUpdated { get; set; }
    public string? FactorType { get; set; }
    public string? Provider { get; set; }
    public string? Status { get; set; }
    public string? VendorName { get; set; }

    [JsonIgnore]
    public string UserId { get; set; } = string.Empty;

    [JsonIgnore]
    public OktaUser? User { get; set; } = null!;

    private OktaUserFactor()
    {
    }

    public OktaUserFactor(UserFactor factor, string userId)
    {
        Id = factor.Id;
        Created = factor.Created;
        LastUpdated = factor.LastUpdated;
        FactorType = factor.FactorType?.Value;
        Provider = factor.Provider;
        Status = factor.Status?.Value;
        VendorName = factor.VendorName;
        UserId = userId;
    }
}
