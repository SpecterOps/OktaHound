using System.ComponentModel.DataAnnotations.Schema;

namespace SpecterOps.OktaHound.Database;

/// <summary>
/// Base class for Okta roles (both built-in and custom).
/// </summary>
public abstract class OktaRole(string id, string name, string domainName) : OktaEntity(id, name, domainName)
{
    public List<string>? Permissions { get; set; }

    [NotMapped]
    public abstract bool IsBuiltIn { get; }
}
