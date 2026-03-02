using System.Text.Json;
using System.Text.Json.Serialization;
using Okta.Sdk.Model;

namespace SpecterOps.OktaHound.Database;

public sealed class OktaRealm : OktaEntity
{
    public const string RealmContainsEdgeKind = "Okta_RealmContains";
    public const string NodeKind = "Okta_Realm";

    public DateTimeOffset? Created { get; set; }
    public DateTimeOffset? LastUpdated { get; set; }
    public bool? IsDefault { get; set; }
    public string? Type { get; set; }
    public List<string>? Domains { get; set; }

    [JsonIgnore]
    public List<OktaUser> Users { get; set; } = [];

    protected override string[] Kinds => [NodeKind];

    private OktaRealm() : base(string.Empty, string.Empty, string.Empty)
    {
    }

    public OktaRealm(Realm realm, string domainName) : base(realm.Id, realm.Profile?.Name ?? realm.Id, domainName)
    {
        DisplayName = realm.Profile?.Name;
        Created = realm.Created;
        LastUpdated = realm.LastUpdated;
        IsDefault = realm.IsDefault;
        Type = realm.Profile?.RealmType?.Value;
        Domains = realm.Profile?.Domains;
    }

    protected override void SerializeProperties(Utf8JsonWriter writer)
    {
        JsonSerializer.Serialize(writer, this, TestSerializationContext.Default.OktaRealm);
    }
}
