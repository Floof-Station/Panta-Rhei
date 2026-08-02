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
This entire file is a mimicry of DeltaV's GhostRoleSystem.Character.cs.
It is made to account for the fact that it can't respond to AntagSelectEntityEvent
and seems to thus be invalid for spawning antagonist ghost roles as the player's character.

This is done by copying the homework of GhostRoleSystem.Character.cs and AntagLoadProfileSystem.cs,
mashing the two together into a horrifying little franken-handler that should get the job done.
This code *should* allow antagonist prototypes to use GhostRoleCharacterSpawnerComponent to
spawn with the player's selected character when it is attached at the same level as AntagSelectionComponent.

This is very likely shitcode. Due to the derivative nature of its creation, it will likely
have unnecessary parts that should not be there. Things may be done in an incorrect or
unstable way. Scrutinize heavily.

If you something weird, it's very likely *not* a design choice, but a mistake.
Please let me know. I am learning :3

With that said, even if this code is safe, it's still a third implementation of essentially the same functionality:

See ghost role needs an entity -> Query player session for information to spawn an appropriate character ->
Spawn a character with the appropriate qualities from the player's selected character.

Is there anything I can do about that? No, not really. AntagLoadProfileRuleSystem's OnSelectEntity handler is from wizden, 
and GhostRoleSystem's OnSpawnerTakeCharacter is from DeltaV. These architectural decisions are above me, but I also wanted
to acknowledge them because I don't like them and want to be up-front about the fact this source file would not need
to exist if when making AntagLoadProfileRule they accounted for a desire to make an antagonist fully into the player's
selected character. Maybe this could be done by making the aspects of the character the system must copy (or must
not copy) a datafield in the component. I did not try to do this because I did not want to interfere with upstream files.
Perhaps even better would be to not separate spawning functionality between antagonists and regular ghost roles.
Even better even better, not separating spawning functionality between any character so that I could just
access the functionality for spawning regular player characters. I think that if that was done, I would
not have had to write any code at all. However, fixing that would involve messing with
some of the most fundamental systems of the game. So... can't do much about that. Rant over :3
*/

namespace Content.Server.Ghost.Roles
{
    public sealed partial class GhostRoleSystem
    {
        private void OnSpawnerTakeAntagSelectedCharacter(Entity<GhostRoleCharacterSpawnerComponent> ent,
            ref AntagSelectEntityEvent args)
        {

            if (args.Handled ||
		!(args.Session != null)) //If the session is null then fuck you I'm outta here
            return;
	    
            var uid = ent.Owner;
            var component = ent.Comp;
	 
	    //The expression here always evaluates to true, unlike in OnSpawnerTakeCharacter.
	    //Maybe something else is handling this? NO idea, I sure hope this isn't important!
            // if (!TryComp(uid, out GhostRoleComponent? ghostRole) ||
            //     ghostRole.Taken)
            // {
            //     //args.TookRole = false;
            //     return;
            // }

	    //Get the player's selected character
            var character = (HumanoidCharacterProfile) _prefs.GetPreferences(args.Session.UserId).SelectedCharacter;
	    
             args.Entity = _ent.System<StationSpawningSystem>()
                 .SpawnPlayerMob(Transform(uid).Coordinates, null, character, null);
             _transform.AttachToGridOrMap(args.Entity.Value);

            var spawnedEvent = new GhostRoleSpawnerUsedEvent(uid, args.Entity.Value, character, args.Session);
            RaiseLocalEvent(args.Entity.Value, spawnedEvent, true);

            EnsureComp<MindContainerComponent>(args.Entity.Value);

	    GhostRoleInternalCreateMindAndTransfer(args.Session, uid, args.Entity.Value);

            _outfit.SetOutfit(args.Entity.Value, component.OutfitPrototype);

            EntityManager.AddComponents(args.Entity.Value, component.AddedComponents);

            if (++component.CurrentTakeovers < component.AvailableTakeovers)
            {
                //args.TookRole = true; //Doesn't exist for AntagSelectEntityEvent
                return;
            }

            // ghostRole.Taken = true; //Doesn't exist for AntagSelectEntityEvent

            if (component.DeleteOnSpawn)
                QueueDel(uid);

            //args.TookRole = true; //Doesn't exist for AntagSelectEntityEvent
        }
    }
}
