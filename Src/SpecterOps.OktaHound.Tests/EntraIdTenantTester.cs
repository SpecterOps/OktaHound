using SpecterOps.OktaHound.Model.Entra;

namespace SpecterOps.OktaHound.Tests;

[TestClass]
public class EntraIdTenantTester
{
    [TestMethod]
    [DataRow("https://login.microsoftonline.com/common/v2.0/.well-known/openid-configuration", "Global")]
    [DataRow("https://login.microsoftonline.us/common/v2.0/.well-known/openid-configuration", "USGovernment")]
    [DataRow("https://login.partner.microsoftonline.cn/common/v2.0/.well-known/openid-configuration", "China")]
    [DataRow("https://example.com/common/v2.0/.well-known/openid-configuration", null)]
    [DataRow(null, null)]
    public void EntraIdTenant_GetRegionFromEndpoint_ReturnsExpectedRegion(string? endpoint, string? expectedRegion)
    {
        var actualRegion = EntraIdTenant.GetRegionFromEndpoint(endpoint);

        Assert.AreEqual(expectedRegion, actualRegion);
    }

    [TestMethod]
    public async Task EntraIdTenant_GetTenantIdFromOnMicrosoftDomain_Empty_ReturnsNull()
    {
        var nullResult = await EntraIdTenant.GetTenantIdFromOnMicrosoftDomain(null);
        var whitespaceResult = await EntraIdTenant.GetTenantIdFromOnMicrosoftDomain("   ");

        Assert.IsNull(nullResult);
        Assert.IsNull(whitespaceResult);
    }

    [TestMethod]
    public async Task EntraIdTenant_GetTenantIdFromOnMicrosoftDomain_Contoso_ReturnsTenantId()
    {
        var tenantId = await EntraIdTenant.GetTenantIdFromOnMicrosoftDomain("contoso");

        Assert.AreEqual("31537af4-6d77-4bb9-a681-d2394888ea26", tenantId);
    }

    [TestMethod]
    public async Task EntraIdTenant_GetTenantIdFromOnMicrosoftDomain_NonExistingDomain_ReturnsNull()
    {
        var tenantId = await EntraIdTenant.GetTenantIdFromOnMicrosoftDomain("thisdomainshouldnotexist12345");

        Assert.IsNull(tenantId);
    }
}
