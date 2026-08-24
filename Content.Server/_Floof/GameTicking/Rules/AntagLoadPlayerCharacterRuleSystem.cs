using Content.Server.Antag;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Preferences.Managers;
using Content.Shared.Preferences;
using Content.Server.Station.Systems;
using Content.Server.Ghost.Roles.Events;

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

        //Get the player's selected character or, if the session is null, whatever the fuck.
        var character = args.Session != null
            ? _prefs.GetPreferences(args.Session.UserId).SelectedCharacter as HumanoidCharacterProfile
            : HumanoidCharacterProfile.RandomWithSpecies();

        //Spawn it like it was a player
        //It seems this function technically spawns the entity somewhere first,
        //but the only purpose of this system is to hand over an entity to
        //AntagSelectEntityEvent. This doesn't matter in any case, but irks me.
        var spawnedCharacter = _ent.System<StationSpawningSystem>()
            .SpawnPlayerMob(Transform(ent.Owner).Coordinates, null, character, null);

        //Create and raise this event so the character is given their traits
        var spawnedEvent = new GhostRoleSpawnerUsedEvent(ent.Owner, spawnedCharacter, character, args.Session);
        RaiseLocalEvent(spawnedCharacter, spawnedEvent, true);

        //Finally, hand over the player's character to the event raiser
        args.Entity = spawnedCharacter;
    }
}
