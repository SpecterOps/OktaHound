namespace SpecterOps.OktaHound.Model.OpenGraph;

internal sealed class OpenGraph(string? sourceKind = null) :
    OpenGraphBase<OpenGraphElements>(OpenGraphSerializationContext.Default, new OpenGraphElements(), sourceKind)
{
}
