using Content.Shared.Body.Components;
using Content.Shared.Whitelist;

namespace Content.Shared.Body.Systems;

public sealed partial class StomachSystem
{
    public void SetSpecialDigestible(StomachComponent stoComp, EntityWhitelist newSpecialDigestible)
    {
        stoComp.SpecialDigestible = newSpecialDigestible;
    }

    public void SetIsSpecialDigestibleExclusive(StomachComponent stoComp, bool newIsSpecialDigestibleExclusive)
    {
        stoComp.IsSpecialDigestibleExclusive = newIsSpecialDigestibleExclusive;
    }
}
