
using System.Linq;
using Content.Shared._Coyote.SniffAndSmell;
using Content.Shared.Administration.Managers;
using Content.Shared.Ghost;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using ScentData = Content.Shared._Coyote.SniffAndSmell.Scent;

namespace Content.Shared._Floof.Scent;

public abstract class SharedScentEditingSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly ISharedAdminManager _admin = default!;

    // TODO cvar? This is roughly the number the traits allowed you to have, but without the lewd/non-lewd separation.
    // If you edit this without making it a cvar your pc will explode at 3 am
    // Also it's hardcoded in ftl, so do something about that too
    public static int MaxScents = 3;

    public List<ScentData> SanitizeScentList(List<ScentData> data)
    {
        if (data.Count > MaxScents)
            data = data.Slice(0, MaxScents);

        return data
            .Select(it => new ScentData(it.ScentProto, Guid.NewGuid().ToString())) // The client could've provided bad guids
            .Where(it => _protoMan.HasIndex(it.ScentProto))
            .Distinct()
            .ToList();
    }

    public bool CanEditScent(ICommonSession session, EntityUid target)
    {
        // Aghosts can edit anyone's smell. Regular players can only edit their own.
        var actor = session.AttachedEntity;
        return actor == target
               || HasComp<GhostComponent>(actor) && _admin.IsAdmin(session);
    }

}
