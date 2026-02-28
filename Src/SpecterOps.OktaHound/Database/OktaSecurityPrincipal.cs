using System.Text.Json.Serialization;

namespace SpecterOps.OktaHound.Database;

public abstract class OktaSecurityPrincipal(string id, string name, string domainName) : OktaEntity(id, name, domainName)
{
    public bool HasRoleAssignments { get; set; }

    [JsonIgnore]
    public bool IsSuperAdmin { get; set; }

    [JsonIgnore]
    public bool IsOrgAdmin { get; set; }
}
