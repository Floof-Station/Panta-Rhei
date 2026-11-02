using Content.Shared._RMC14.CCVar;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using HumanoidAppearanceComponent = Content.Shared.Humanoid.HumanoidProfileComponent; // Euph, renamed upstream

namespace Content.Shared._RMC14.Voicelines;

public sealed class HumanoidVoicelinesSystem : EntitySystem
{
    [Dependency] private readonly INetConfigurationManager _config = default!;

    private static readonly ProtoId<SpeciesPrototype> ArachnidSpecies = "Arachnid";
    private static readonly ProtoId<SpeciesPrototype> DionaSpecies = "Diona";
    // private static readonly ProtoId<SpeciesPrototype> DwarfSpecies = "Dwarf"; // Euph, no dwarves
    private static readonly ProtoId<SpeciesPrototype> FelinidSpecies = "Felinid"; // Harmony - No Felenids // Euph, yes Felenids
    private static readonly ProtoId<SpeciesPrototype> HumanSpecies = "Human";
    private static readonly ProtoId<SpeciesPrototype> MothSpecies = "Moth";
    private static readonly ProtoId<SpeciesPrototype> ReptilianSpecies = "Reptilian";
    private static readonly ProtoId<SpeciesPrototype> SlimeSpecies = "SlimePerson";
    private static readonly ProtoId<SpeciesPrototype> AvaliSpecies = "Avali";
    private static readonly ProtoId<SpeciesPrototype> VulpkaninSpecies = "Vulpkanin"; // Harmony - No Vulps // Euph, yes Vulps
    private static readonly ProtoId<SpeciesPrototype> RodentiaSpecies = "Rodentia"; // Harmony - No Rodentia // Euph, yes Rodentia
    private static readonly ProtoId<SpeciesPrototype> FeroxiSpecies = "Feroxi"; // Harmony - No Feroxi // Euph, yes Feroxi
    // private static readonly ProtoId<SpeciesPrototype> SkrellSpecies = "Skrell"; // Harmony - No Skrell
    private static readonly ProtoId<SpeciesPrototype> ArachneSpecies = "Arachne"; // Euph
    private static readonly ProtoId<SpeciesPrototype> AllulaloSpecies = "Allulalo"; // Euph
    private static readonly ProtoId<SpeciesPrototype> ChitinidSpecies = "Chitinid"; // Euph
    private static readonly ProtoId<SpeciesPrototype> HarpySpecies = "Harpy"; // Euph
    private static readonly ProtoId<SpeciesPrototype> IPCSpecies = "IPC"; // Euph
    private static readonly ProtoId<SpeciesPrototype> KitsuneSpecies = "Kitsune"; // Euph
    private static readonly ProtoId<SpeciesPrototype> MotorkindSpecies = "Motorkind"; // Euph
    private static readonly ProtoId<SpeciesPrototype> OniSpecies = "Oni"; // Euph
    private static readonly ProtoId<SpeciesPrototype> ResomiSpecies = "Resomi"; // Euph
    private static readonly ProtoId<SpeciesPrototype> ShadekinSpecies = "Shadekin"; // Euph
    private static readonly ProtoId<SpeciesPrototype> TajaranSpecies = "Tajaran"; // Euph
    private static readonly ProtoId<SpeciesPrototype> ThavenSpecies = "Thaven"; // Euph
    private static readonly ProtoId<SpeciesPrototype> VoxSpecies = "Vox"; // Euph

    private readonly Dictionary<ProtoId<SpeciesPrototype>, CVarDef> _voicelineCVars = new()
    {
        [ArachnidSpecies] = RMCCVars.RMCPlayVoicelinesArachnid,
        [DionaSpecies] = RMCCVars.RMCPlayVoicelinesDiona,
        // [DwarfSpecies] = RMCCVars.RMCPlayVoicelinesDwarf, // Euph, no dwarves
        [FelinidSpecies] = RMCCVars.RMCPlayVoicelinesFelinid, // Harmony - No Felenids // Euph, yes Felenids
        [HumanSpecies] = RMCCVars.RMCPlayVoicelinesHuman,
        [MothSpecies] = RMCCVars.RMCPlayVoicelinesMoth,
        [ReptilianSpecies] = RMCCVars.RMCPlayVoicelinesReptilian,
        [SlimeSpecies] = RMCCVars.RMCPlayVoicelinesSlime,
        [AvaliSpecies] = RMCCVars.RMCPlayVoicelinesAvali,
        [VulpkaninSpecies] = RMCCVars.RMCPlayVoicelinesVulpkanin, // Harmony - No Vulps // Euph, yes Vulps
        [RodentiaSpecies] = RMCCVars.RMCPlayVoicelinesRodentia, // Harmony - No Rodentia // Euph, yes Rodentia
        [FeroxiSpecies] = RMCCVars.RMCPlayVoicelinesFeroxi, // Harmony - No Feroxi // Euph, yes Feroxi
        // [SkrellSpecies] = RMCCVars.RMCPlayVoicelinesSkrell, // Harmony - No Skrell
        [ArachneSpecies] = RMCCVars.RMCPlayVoicelinesArachne, // Euph
        [AllulaloSpecies] = RMCCVars.RMCPlayVoicelinesAllulalo, // Euph
        [ChitinidSpecies] = RMCCVars.RMCPlayVoicelinesChitinid, // Euph
        [HarpySpecies] = RMCCVars.RMCPlayVoicelinesHarpy, // Euph
        [IPCSpecies] = RMCCVars.RMCPlayVoicelinesIPC, // Euph
        [KitsuneSpecies] = RMCCVars.RMCPlayVoicelinesKitsune, // Euph
        [MotorkindSpecies] = RMCCVars.RMCPlayVoicelinesMotorkind, // Euph
        [OniSpecies] = RMCCVars.RMCPlayVoicelinesOni, // Euph
        [ResomiSpecies] = RMCCVars.RMCPlayVoicelinesResomi, // Euph
        [ShadekinSpecies] = RMCCVars.RMCPlayVoicelinesShadekin, // Euph
        [TajaranSpecies] = RMCCVars.RMCPlayVoicelinesTajaran, // Euph
        [ThavenSpecies] = RMCCVars.RMCPlayVoicelinesThaven, // Euph
        [VoxSpecies] = RMCCVars.RMCPlayVoicelinesVox, // Euph
    };

    private readonly Dictionary<ProtoId<SpeciesPrototype>, CVarDef> _emoteCVars = new()
    {
        [ArachnidSpecies] = RMCCVars.RMCPlayEmotesArachnid,
        [DionaSpecies] = RMCCVars.RMCPlayEmotesDiona,
        // [DwarfSpecies] = RMCCVars.RMCPlayEmotesDwarf, // Euph, no dwarves
        [FelinidSpecies] = RMCCVars.RMCPlayEmotesFelinid, // Harmony - No Felenids // Euph, yes Felenids
        [HumanSpecies] = RMCCVars.RMCPlayEmotesHuman,
        [MothSpecies] = RMCCVars.RMCPlayEmotesMoth,
        [ReptilianSpecies] = RMCCVars.RMCPlayEmotesReptilian,
        [SlimeSpecies] = RMCCVars.RMCPlayEmotesSlime,
        [AvaliSpecies] = RMCCVars.RMCPlayEmotesAvali,
        [VulpkaninSpecies] = RMCCVars.RMCPlayEmotesVulpkanin, // Harmony - No Vulps // Euph, yes Vulps
        [RodentiaSpecies] = RMCCVars.RMCPlayEmotesRodentia, // Harmony - No Rodentia // Euph, yes Rodentia
        [FeroxiSpecies] = RMCCVars.RMCPlayEmotesFeroxi, // Harmony - No Feroxi // Euph, yes Feroxi
        // [SkrellSpecies] = RMCCVars.RMCPlayEmotesSkrell, // Harmony - No Skrell
        [ArachneSpecies] = RMCCVars.RMCPlayEmotesArachne, // Euph
        [AllulaloSpecies] = RMCCVars.RMCPlayEmotesAllulalo, // Euph
        [ChitinidSpecies] = RMCCVars.RMCPlayEmotesChitinid, // Euph
        [HarpySpecies] = RMCCVars.RMCPlayEmotesHarpy, // Euph
        [IPCSpecies] = RMCCVars.RMCPlayEmotesIPC, // Euph
        [KitsuneSpecies] = RMCCVars.RMCPlayEmotesKitsune, // Euph
        [MotorkindSpecies] = RMCCVars.RMCPlayEmotesMotorkind, // Euph
        [OniSpecies] = RMCCVars.RMCPlayEmotesOni, // Euph
        [ResomiSpecies] = RMCCVars.RMCPlayEmotesResomi, // Euph
        [ShadekinSpecies] = RMCCVars.RMCPlayEmotesShadekin, // Euph
        [TajaranSpecies] = RMCCVars.RMCPlayEmotesTajaran, // Euph
        [ThavenSpecies] = RMCCVars.RMCPlayEmotesThaven, // Euph
        [VoxSpecies] = RMCCVars.RMCPlayEmotesVox, // Euph
    };

    private EntityQuery<HumanoidAppearanceComponent> _humanoidAppearanceQuery;

    public override void Initialize()
    {
        _humanoidAppearanceQuery = GetEntityQuery<HumanoidAppearanceComponent>();
    }

    public bool ShouldPlayVoiceline(Entity<HumanoidAppearanceComponent?> vocalizer, ICommonSession forPlayer)
    {
        if (forPlayer.AttachedEntity == vocalizer &&
            !_config.GetClientCVar(forPlayer.Channel, RMCCVars.RMCPlayVoicelinesYourself))
        {
            return false;
        }

        if (!_humanoidAppearanceQuery.Resolve(vocalizer, ref vocalizer.Comp, false) ||
            !_voicelineCVars.TryGetValue(vocalizer.Comp.Species, out var play))
        {
            return true;
        }

        return _config.GetClientCVar<bool>(forPlayer.Channel, play.Name);
    }

    public bool ShouldPlayEmote(Entity<HumanoidAppearanceComponent?> vocalizer, ICommonSession forPlayer)
    {
        if (forPlayer.AttachedEntity == vocalizer &&
            !_config.GetClientCVar(forPlayer.Channel, RMCCVars.RMCPlayEmotesYourself))
        {
            return false;
        }

        if (!_humanoidAppearanceQuery.Resolve(vocalizer, ref vocalizer.Comp, false) ||
            !_emoteCVars.TryGetValue(vocalizer.Comp.Species, out var play))
        {
            return true;
        }

        return _config.GetClientCVar<bool>(forPlayer.Channel, play.Name);
    }

// Harmony Change Start
    public bool ShouldPlayVoicelines(Entity<HumanoidAppearanceComponent?> vocalizer, ICommonSession forPlayer)
    {
        if (forPlayer.AttachedEntity == vocalizer &&
            !_config.GetClientCVar(forPlayer.Channel, RMCCVars.RMCPlayVoicelinesYourself))
        {
            return false;
        }

        if (!_humanoidAppearanceQuery.Resolve(vocalizer, ref vocalizer.Comp, false) ||
            !_voicelineCVars.TryGetValue(vocalizer.Comp.Species, out var play))
        {
            return true;
        }
// Harmony Change End

        return _config.GetClientCVar<bool>(forPlayer.Channel, play.Name);
    }
}