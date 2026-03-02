using Okta.Sdk.Model;

namespace SpecterOps.OktaHound.Database;

public sealed class OktaPrivilegedUser
{
    public string Id { get; set; } = string.Empty;

    public string? Orn { get; set; }

    public OktaUser User { get; set; } = null!;

    private OktaPrivilegedUser()
    {
    }

    public OktaPrivilegedUser(RoleAssignedUser privilegedUser)
    {
        Id = privilegedUser.Id;
        Orn = privilegedUser.Orn;
    }
}
