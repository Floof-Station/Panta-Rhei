# Euphoria Contributing Guidelines

###### _(Note that this has been largely borrowed from Delta-V. In the future, we will replace most of these examples with examples based on our native developments. For now, this is a placeholder. - M3739)_

Generally we follow [Wizden's PR guidelines](https://docs.spacestation14.com/en/general-development/codebase-info/pull-request-guidelines.html) for code quality and such.

Importantly do not make webedits, copied verbatim from above:
> Do not use GitHub's web editor to create PRs. PRs submitted through the web editor may be closed without review.

Upstream is the [DeltaV-Station/Delta-v](https://github.com/DeltaV-Station/Delta-v) repository that Delta-V runs on.

# Content specific to Euphoria

In general anything you create from scratch (not modifying something that exists from upstream) should go in the Euphoria subfolder, `_Euphoria`.

###### _(Remind me to redo this section to feature our native developments as examples. - M3739)_

Examples:
- `Content.Server/_DV/Chapel/SacrificialAltarSystem.cs`
- `Resources/Prototypes/_DV/ai_factions.yml`
- `Resources/Audio/_DV/Items/gavel.ogg`
- `Resources/Textures/_DV/Icons/cri.rsi`
- `Resources/Locale/en-US/_DV/shipyard/shipyard-console.ftl`
- `Resources/ServerInfo/Guidebook/_DV/AlertProcedure.xml`
  Note that guidebooks go in `ServerInfo/Guidebook/_Floof` and not `ServerInfo/_Floof`!

# Changes to upstream files

Follow a few guidelines when modifying non-Euphoria files, to help us manage our project. (files that are not in the `_Euphoria` folder)

Primarily, **add comments on or around all new or changed lines** in upstream files. Explain what was changed to make resolving merge conflicts easier; we regularly merge new upstream changes into our project.

### Changing Upstream YAML .yml files

**Add comments on or around any changed lines.**

If you add a new component to a prototype, add an explanation to the `type: ...` line. Example:

```yml
- type: entity
  parent: MobSiliconBase
  id: MobSupplyBot
  components:
  - type: InteractionPopup # Euphoria - Make supplybots pettable
    interactSuccessString: petting-success-supplybot
    interactFailureString: petting-failure-supplybot
    interactSuccessSound:
      path: /Audio/Ambience/Objects/periodic_beep.ogg
```

Whereas if you just modify some fields of a component, comment the fields instead, using inline or block comments. Examples:

```yml
- type: entityTable
  id: FillLockerWarden
  table: !type:AllSelector
    children:
    - id: ClothingHandsGlovesCombat
    - id: ClothingShoesBootsSecurityMagboots # Euphoria - Added security magboots.
    - id: ClothingShoesBootsJack
    #- id: ClothingOuterCoatWarden # Euphoria - removed for incongruence
    #- id: ClothingOuterWinterWarden # Euphoria - removed for incongruence
    - id: RubberStampWarden
    - id: DoorRemoteArmory
    - id: HoloprojectorSecurity
    # Begin Euphoria additions
    - id: WeaponEnergyShotgun
    - id: BoxPDAPrisoner
    - id: LunchboxSecurityFilledRandom
      prob: 0.3
    # End Euphoria additions
```

### Changing Upstream C# .cs files

If you are adding a lot of C# code, then take advantage of partial classes. Put the new code in its own file in the `_Euphoria` folder, if it makes sense.

Otherwise, **add comments on or around any changed lines.**

Imports (using statements) are excempt as Rider's auto-import and import optimization features make it trivial to resolve conflicts related to changes in those lines.

### Single-Line Changes
Format should look like this.
```cs
/* DO NOT COMMENT CHANGES TO IMPORT STATEMENTS! Those are redundant as Rider can auto-import and will be lost if someone reformats the file. */
using Content.Server._DV.Psionics.Glimmer;
using Content.Shared.Damage.Systems;

/* Changing an upstream line - Same line as the change */
if (!TryComp<EyeComponent>(ent, out var eye) || _disabled) // DeltaV - check if disabled

/* Adding - Either same line or above the line. */
  EnsureComp<PotentialPsionicComponent>(entity); // Deltav - Psionics

/* "Deleting" - Don't actually delete, just comment out and say why. This only applies to upstream code. */
// args.StatusIcons.Add(_prototype.Index(component.Icon)); // DeltaV - commented out. status icon now added above
```

> * Its pretty obvious in the example above that importing `Content.Server._DV.Psionics.Glimmer` means we'll be interacting with glimmer so putting `// DeltaV - Add Glimmer` is needlessly redundant.
> * It's not as obvious what the `Content.Shared.Damage.Systems` namespace is used for, since its so broad, so adding a comment what feature is using it helps.
> * Actual code changes should almost always include the comment after ``// DeltaV`.

### Multi-Line Changes
Depending on how much you are editing, putting a comment on EACH line may be excessive, so if you have a larger block of code you are changing, denote it like so:
```cs
// BEGIN DeltaV - Remove innate radio and radios from pockets
for (var i = 1; i <= 4; i++) // Arachnids have 4 pockets
{
    if (_inventory.TryGetSlotEntity(target, $"pocket{i}", out var headset) && HasComp<HeadsetComponent>(headset))
        _inventory.TryUnequip(target, $"pocket{i}", true, true);
}

RemComp<ActiveRadioComponent>(target); // If the zombie has an innate radio, get rid of it.
// END DeltaV
```

> * Denoting these with a BEGIN and END clearly shows they are block of code without having to read the entire comment. This makes it easier to tell when you're dealing with single-line comments versus a block with merging in conflicts.
>   * Case and order of the first two words is less of a concern. `// DeltaV Begin` or `// Begin DeltaV` will work fine too.
> * Try to make your blocks as small as possible, but use your discretion.
> * If you deleting multiple lines, use line comments (``//``) if its a few lines but if its a larger block (like commenting out an entire function), it is preferable to use block comments (`/* */`).

#### Soft Exceptions to the Multi-Line "Rules"
Some multi-line changes can use a single-line comment in certain scenarios. But if you are UNSURE, just use `// BEGIN DeltaV` and `// END DeltaV` comments like the previous section does and it'll be fine.

I'll give some examples.
```cs
/* This change comments out 3 lines but only needs a single line comment because commenting out the if statement implies that its logic will be commented out too. */
// if (obj.WasModified<TraitPrototype>()) // DeltaV - Refreshed in TraitsTab
// {
//     _profileEditor.RefreshTraits();
// }

/* Same principle here. This adds two lines but the if statement implies the next line so commenting both lines isn't really needed. */
if (_flight.IsFlying(entity.Owner)) // DeltaV - Harpy Flight
    return true;
```
### New Methods or Component Variables
Sometimes, you'll need to implement a whole new method or component variable and instead of wrapping it in `// BEGIN DeltaV` and `// END DeltaV`, you can just denote that it's a DeltaV function in the summary block before the function. This denotes the WHOLE function as a DeltaV addition.
```cs
/* New Method Example */
/// <summary>
/// DeltaV - Handle revealing ninja if cloaked when attacked by a hitscan attack.
/// </summary>
private void OnNinjaAttacked(Entity<SpaceNinjaComponent> ent, ref DamageChangedEvent args)
{
  ...
}

/* New Component Variable Example */
/// <summary>
/// DeltaV - If disabled the action will not disable when no charges remain. Use if you want to handle no charges differently.
/// </summary>
[DataField]
public bool DisableWhenEmpty = true;
```

In short:
* Use `// BEGIN DeltaV` and `// END Delta` to denote a *block* of changes.
  * Keep blocks as small as possible.
* Use `// DeltaV` on or before the line if its not a block of changes.
* Use exceptions when they make sense.

### Changing Upstream Localization Fluent .ftl files

**Move all changed locale strings to a new Euphoria file** - use a `.ftl` file in the `_Euphoria` folder. Comment out the old strings in the upstream file, and explain that they were moved.

Example:

Commented out old string in `Resources\Locale\en-US\xenoarchaeology\artifact-analyzer.ftl`
```
# Euphoria - moved to _Euphoria file
# analysis-console-info-effect-value = [font="Monospace" size=11][color=gray]{ $state ->
#     [true] {$info}
#     *[false] Unlock nodes to gain info
# }[/color][/font]
```

The new version of the string in `Resources\Locale\en-US\_Euphoria\xenoarchaeology\artifact-analyzer.ftl`
```
analysis-console-info-effect-value = [font="Monospace" size=11][color=gray]{ $state ->
    [vagueandspecific] {$vagueInfo} ({$specificInfo})
    [vagueonly] {$vagueInfo} (unable to detect details)
    [simple] {$specificInfo}
    [hidden] Unable to detect (unlock to discover)
    *[noinfo] Unlock nodes to gain info
}[/color][/font]
```

Also keep in mind that fluent (.ftl) files **do not support comments on the same line** as a locale value, so be careful when commenting.

### Early merges

We mostly merge upstream changes in big chunks (e.g. a month of upstream PRs at a time), but urgent changes can be merged early, separately.

Early merges are an exception to the above rules - if cherry-picking a PR for an early merge, you don't need to add `#DeltaV` comments, since the code is coming directly from upstream without any changes.

# Mapping

If you want to make changes to a map, get in touch with its maintainer to make sure you don't both make changes at the same time.

Conflicts with maps make PRs mutually exclusive so either your work or the maintainer's work will be lost, communicate to avoid this!

Please make a detailed list of **all** changes(even minor changes) with locations when submitting a PR. This helps reviewers hone in on them without having to search an entire map for differences. Ex: [Map Edits](https://github.com/DeltaV-Station/Delta-v/pull/3165)


**Submitting a map PR**

Please limit changelogs on map PRs to **significant** map alterations or additions. Minor map edits do not need changelogs.
Format for map PRs looks like:
```
:cl: Yourname
MAPS:
- add: Mapname: Added fun!
- remove: Mapname: Removed fun!
- tweak: Mapname: Changed fun!
- fix: Mapname: Fixed fun!
```

# Before you submit

Double-check your diff on GitHub before submitting: look for unintended commits or changes and remove accidental whitespace or line-ending changes.

Additionally for long-lasting PRs, if you see `RobustToolbox` in the changed files you have to revert it, use `git checkout upstream/master RobustToolbox` (replacing `upstream` with the name of your Floof-Station/Panta-Rhei remote)

# Changelogs
###### _(This section is not yet implemented. DO NOT ATTEMPT TO USE THE ADMIN OR MAPS CHANGELOGS. - M3739)_

By default any changelogs goes in the Euphoria changelog, you can use the Euphoria admin changelog by putting `DELTAVADMIN:` in a line after `:cl:`.

Do not use `ADMIN:` as **it will mangle** the upstream admin changelog!

# Additional resources

If you are new to contributing to SS14 in general, have a look at the [SS14 docs](https://docs.spacestation14.io/) or ask for help in `#Development` on [Discord](https://discord.gg/pmxfNx8gRS)!

## AI-Generated Content
Code, sprites and any other AI-generated content is not allowed to be submitted to the repository.

Trying to PR AI-generated content may result in you being banned from contributing.
