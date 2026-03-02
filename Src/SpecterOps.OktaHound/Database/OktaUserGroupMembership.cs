namespace SpecterOps.OktaHound.Database;

public sealed class OktaUserGroupMembership
{
    public string UserId { get; set; } = string.Empty;

    public string GroupId { get; set; } = string.Empty;

    public OktaUser User { get; set; } = null!;

    public OktaGroup Group { get; set; } = null!;
}
