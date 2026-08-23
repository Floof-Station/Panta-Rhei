using Content.Server.Antag;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Preferences.Managers;
using Content.Shared.Preferences;
using Content.Server.Station.Systems;

namespace Content.Server.GameTicking.Rules;

/// <summary>
/// System that responds to AntagSelectEntityEvent, gets the player's currently selected character, spawns
/// it in the same way regular players are spawned, and sets args.Entity to the result. This is dinstinct functionality from 
/// <see cref="AntagLoadProfileRuleSystem"/> in that while that system gets the appearance of the player's character,
/// this one is for when you want the whole character, name, traits, and all.
/// </summary>
public sealed class AntagLoadPlayerCharacterRuleSystem : GameRuleSystem<AntagLoadPlayerCharacterRuleComponent>
{
    [Dependency] private readonly IServerPreferencesManager _prefs = default!;
    [Dependency] private readonly IEntityManager _ent = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AntagLoadPlayerCharacterRuleComponent, AntagSelectEntityEvent>(OnSelectEntity);
    }

    private void OnSelectEntity(Entity<AntagLoadPlayerCharacterRuleComponent> ent, ref AntagSelectEntityEvent args)
    {
        if (args.Handled) //If something already handled this, don't handle it! 
            return;
	    
	var uid = ent.Owner;


	//Get the player's selected character or, if the session is null, whatever the fuck.
	var character = args.Session != null
	    ? _prefs.GetPreferences(args.Session.UserId).SelectedCharacter as HumanoidCharacterProfile
	    : HumanoidCharacterProfile.RandomWithSpecies();

	//Spawn it like it was a player
	//It seems this function technically spawns the entity somewhere first,
	//but the only purpose of this system is to hand over an entity to
	//AntagSelectEntityEvent. This doesn't matter in any case, but irks me.
	args.Entity = _ent.System<StationSpawningSystem>()
	    .SpawnPlayerMob(Transform(uid).Coordinates, null, character, null);
    }
}
