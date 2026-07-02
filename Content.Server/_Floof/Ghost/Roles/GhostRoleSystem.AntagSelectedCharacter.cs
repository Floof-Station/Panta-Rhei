using Content.Server.Administration.Commands;
using Content.Server.Clothing.Systems;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Ghost.Roles.Events;
using Content.Server.Preferences.Managers;
using Content.Server.Station.Systems;
using Content.Shared.Mind.Components;
using Content.Shared.Preferences;
using Content.Shared.GameTicking;
using Content.Server.Database;
using Content.Server.Antag;
using Robust.Shared.Player;
using Content.Server.Humanoid;
/*
This entire file is a mimicry of DeltaV's GhostRoleSystem.Character.cs, made to account for the fact that it can't respond to
AntagSelectEntityEvent and seems to thus be invalid for spawning antagonist ghost roles as the player's character.
Please be aware that this code was added by a teenager who read most of* a book on C#. Scrutinize heavily. If you something weird, it's very likely *not* a design choice, but a mistake. Please let me know.
*/

namespace Content.Server.Ghost.Roles
{
    public sealed partial class GhostRoleSystem
    {
        //[Dependency] private readonly IServerPreferencesManager _prefs = default!;
        //[Dependency] private readonly OutfitSystem _outfit = default!;
	[Dependency] private readonly HumanoidAppearanceSystem _humanoid = default!;

        private void OnSpawnerTakeAntagSelectedCharacter(Entity<GhostRoleCharacterSpawnerComponent> ent,
            ref AntagSelectEntityEvent args)
        {

            if (args.Handled ||
		!(args.Session != null)) //If the session is null then fuck you I'm outta here
            return;
	    
            var uid = ent.Owner;
            var component = ent.Comp;
	 
            if (!TryComp(uid, out GhostRoleComponent? ghostRole) ||
                ghostRole.Taken)
            {
                //args.TookRole = false;
                return;
            }

            var character = (HumanoidCharacterProfile) _prefs.GetPreferences(args.Session.UserId).SelectedCharacter;
	    
             args.Entity = _ent.System<StationSpawningSystem>()
                 .SpawnPlayerMob(Transform(uid).Coordinates, null, character, null);
             _transform.AttachToGridOrMap(args.Entity.Value);

            var spawnedEvent = new GhostRoleSpawnerUsedEvent(uid, args.Entity.Value, character, args.Session);
            RaiseLocalEvent(args.Entity.Value, spawnedEvent, true);

            EnsureComp<MindContainerComponent>(args.Entity.Value);

	    GhostRoleInternalCreateMindAndTransfer(args.Session, uid, args.Entity.Value, ghostRole);

            _outfit.SetOutfit(args.Entity.Value, component.OutfitPrototype);

            EntityManager.AddComponents(args.Entity.Value, component.AddedComponents);

            if (++component.CurrentTakeovers < component.AvailableTakeovers)
            {
                //args.TookRole = true;
                return;
            }

            ghostRole.Taken = true;

            if (component.DeleteOnSpawn)
                QueueDel(uid);

            //args.TookRole = true;
        }
    }
}
