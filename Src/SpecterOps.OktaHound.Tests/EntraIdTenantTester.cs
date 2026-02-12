using SpecterOps.OktaHound.Model.Entra;

namespace SpecterOps.OktaHound.Tests;

[TestClass]
public class EntraIdTenantTester
{
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
