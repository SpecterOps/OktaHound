using System.Text.Json;
using SpecterOps.OktaHound.Model.Entra;
using SpecterOps.OktaHound.Model.OpenGraph;

namespace SpecterOps.OktaHound.Tests;

[TestClass]
public class EntraIdUserTester
{
    [TestMethod]
    public void EntraIdUser_CreateEdgeNode_Serialize()
    {
        var entraUser = EntraIdUser.CreateEdgeNode("alice@contoso.com", "924feb32-9cad-4276-a55b-981788e5b31a");
        var expectedJson = "{\"properties\":{\"userPrincipalName\":\"alice@contoso.com\",\"tenantId\":\"924feb32-9cad-4276-a55b-981788e5b31a\"},\"match_by\":\"properties\",\"kind\":\"AZUser\"}";

        string json = JsonSerializer.Serialize(entraUser, typeof(OpenGraphEdgeNode), OpenGraphSerializationContext.Default);

        Assert.AreEqual(expectedJson, json);
    }
}
