using Content.Shared.Administration;
using Content.Shared.CCVar.CVarAccess;
using Robust.Shared;
using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

/// <summary>
/// Contains all the CVars used by content.
/// </summary>
/// <remarks>
/// NOTICE FOR FORKS: Put your own CVars in a separate file with a different [CVarDefs] attribute. RT will automatically pick up on it.
/// </remarks>
[CVarDefs]
public sealed partial class CCVars : CVars
{
    // Only debug stuff lives here.

#if DEBUG
    [CVarControl(AdminFlags.Debug)]
    public static readonly CVarDef<string> DebugTestCVar =
        CVarDef.Create("debug.test_cvar", "default", CVar.SERVER);

    [CVarControl(AdminFlags.Debug)]
    public static readonly CVarDef<float> DebugTestCVar2 =
        CVarDef.Create("debug.test_cvar2", 123.42069f, CVar.SERVER);
#endif

    #region Contests System

        /// <summary>
        ///     The MASTER TOGGLE for the entire Contests System.
        ///     ALL CONTESTS BELOW, regardless of type or setting will output 1f when false.
        /// </summary>
        public static readonly CVarDef<bool> DoContestsSystem =
            CVarDef.Create("contests.do_contests_system", true, CVar.REPLICATED | CVar.SERVER);

        /// <summary>
        ///     Contest functions normally include an optional override to bypass the clamp set by max_percentage.
        ///     This CVar disables the bypass when false, forcing all implementations to comply with max_percentage.
        /// </summary>
        public static readonly CVarDef<bool> AllowClampOverride =
            CVarDef.Create("contests.allow_clamp_override", true, CVar.REPLICATED | CVar.SERVER);
        /// <summary>
        ///     Toggles all MassContest functions. All mass contests output 1f when false
        /// </summary>
        public static readonly CVarDef<bool> DoMassContests =
            CVarDef.Create("contests.do_mass_contests", true, CVar.REPLICATED | CVar.SERVER);

        /// <summary>
        ///     Toggles all StaminaContest functions. All stamina contests output 1f when false
        /// </summary>
        public static readonly CVarDef<bool> DoStaminaContests =
            CVarDef.Create("contests.do_stamina_contests", true, CVar.REPLICATED | CVar.SERVER);

        /// <summary>
        ///     Toggles all HealthContest functions. All health contests output 1f when false
        /// </summary>
        public static readonly CVarDef<bool> DoHealthContests =
            CVarDef.Create("contests.do_health_contests", true, CVar.REPLICATED | CVar.SERVER);

        /// <summary>
        ///     Toggles all MindContest functions. All mind contests output 1f when false.
        ///     MindContests are not currently implemented, and are awaiting completion of the Psionic Refactor
        /// </summary>
        public static readonly CVarDef<bool> DoMindContests =
            CVarDef.Create("contests.do_mind_contests", true, CVar.REPLICATED | CVar.SERVER);

        /// <summary>
        ///     The maximum amount that Mass Contests can modify a physics multiplier, given as a +/- percentage
        ///     Default of 0.25f outputs between * 0.75f and 1.25f
        /// </summary>
        public static readonly CVarDef<float> MassContestsMaxPercentage =
            CVarDef.Create("contests.max_percentage", 0.25f, CVar.REPLICATED | CVar.SERVER);

    #endregion

    /// <summary>
    /// A simple toggle to test <c>OptionsVisualizerComponent</c>.
    /// </summary>
    public static readonly CVarDef<bool> DebugOptionVisualizerTest =
        CVarDef.Create("debug.option_visualizer_test", false, CVar.CLIENTONLY);

    /// <summary>
    /// Set to true to disable parallel processing in the pow3r solver.
    /// </summary>
    public static readonly CVarDef<bool> DebugPow3rDisableParallel =
        CVarDef.Create("debug.pow3r_disable_parallel", false, CVar.SERVERONLY);

    /// <summary>
    ///     Goobstation: The amount of time between NPC Silicons draining their battery in seconds.
    ///     TODO: Move this to the proper file. I dunno which, just not the main one.
    /// </summary>
    public static readonly CVarDef<float> SiliconNpcUpdateTime =
        CVarDef.Create("silicon.npcupdatetime", 1.5f, CVar.SERVERONLY);
}
