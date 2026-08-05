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

    private void OnSelectEntity(Entity<AntagLoadProfileRuleComponent> ent, ref AntagSelectEntityEvent args)
    {
        if (args.Handled ||	(args.Session == null)) //If the session is null then fuck you I'm outta here
            return;
	    
            var uid = ent.Owner;
            var component = ent.Comp;


	    //Get the player's selected character

	    var character = args.Session != null
		? _prefs.GetPreferences(args.Session.UserId).SelectedCharacter as HumanoidCharacterProfile
		: HumanoidCharacterProfile.RandomWithSpecies();

	    
             args.Entity = _ent.System<StationSpawningSystem>()
                 .SpawnPlayerMob(Transform(uid).Coordinates, null, character, null);

            //EnsureComp<MindContainerComponent>(args.Entity.Value); //Perhaps unnecessary?
    }
}
