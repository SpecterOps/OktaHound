using System.Text.Json.Serialization;

namespace SpecterOps.OktaHound.Database;

public abstract class OktaEntity(string id, string name, string domainName) : OpenGraphEntity(id, name)
{
    [JsonPropertyName("environment_name")]
    public string DomainName { get; set; } = domainName;

    [JsonIgnore]
    public OktaOrganization OktaOrganization { get; set; } = null!;
}
