using Content.Server.NPC.Systems;
using Content.Shared._Euphoria.NPC;

namespace Content.Server._Euphoria.NPC;

public sealed class HtnHelperSystem : SharedHtnHelperSystem
{
    [Dependency] private readonly NPCSystem _npc = default!;

    public override void SetBlackboard(EntityUid uid, string key, object value)
    {
        _npc.SetBlackboard(uid, key, value);
    }
}
