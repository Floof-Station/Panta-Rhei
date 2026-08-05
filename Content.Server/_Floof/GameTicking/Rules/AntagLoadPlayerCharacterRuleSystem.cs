using Content.Server.Antag;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Humanoid;
using Content.Server.Preferences.Managers;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Preferences;
using Robust.Shared.Prototypes;
using Content.Server.Station.Systems;

namespace Content.Server.GameTicking.Rules;

public sealed class AntagLoadPlayerCharacterRuleSystem : GameRuleSystem<AntagLoadPlayerCharacterRuleComponent>
{
    [Dependency] private readonly HumanoidAppearanceSystem _humanoid = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
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
	var component = ent.Comp;


	//Get the player's selected character or, if the session is null, whatever the fuck.
	var character = args.Session != null
	    ? _prefs.GetPreferences(args.Session.UserId).SelectedCharacter as HumanoidCharacterProfile
	    : HumanoidCharacterProfile.RandomWithSpecies();

	//Spawn it like it was a player
	//Where does this spawn it at first? Hell if I know. It ends up where it's supposed to be anyways.
	args.Entity = _ent.System<StationSpawningSystem>()
	    .SpawnPlayerMob(Transform(uid).Coordinates, null, character, null);
    }
}
