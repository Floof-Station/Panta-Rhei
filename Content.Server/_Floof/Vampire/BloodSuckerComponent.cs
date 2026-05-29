using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;


namespace Content.Server._Floof.Vampire
{
    [RegisterComponent]
    public sealed partial class BloodSuckerComponent : Component
    {
        /// <summary>
        /// How much to succ each time we succ.
        /// </summary>
        [DataField("unitsToSucc")]
        public float UnitsToSucc = 20f;

        /// <summary>
        /// The time (in seconds) that it takes to succ an entity.
        /// </summary>
        [DataField, ViewVariables(VVAccess.ReadWrite)]
        public TimeSpan Delay = TimeSpan.FromSeconds(4);
// Hardlight unused - start
        // ***INJECT WHEN SUCC***

        /// <summary>
        /// Whether to inject chems into a chemstream when we suck something.
        /// </summary>
        // [DataField("injectWhenSucc")]
        // public bool InjectWhenSucc = false;

        /// <summary>
        /// How many units of our injected chem to inject.
        /// </summary>
        // [DataField("unitsToInject")]
        // public float UnitsToInject = 5;

        /// <summary>
        /// Which reagent to inject.
        /// </summary>
        // [DataField("injectReagent")]
        // public string InjectReagent = "";

        /// <summary>
        /// Whether we need to web the thing up first...
        /// </summary>
        // [DataField("webRequired")]
        // public bool WebRequired = false;

        /// <summary>
        ///     DEN: Used to track BloodExaminer, but only if it is added by this component.
        /// </summary>
        // public BloodExaminerComponent? AddedBloodExaminer;
// Hardlight unused - end
        /// <summary>
        /// The sound made when drinking blood.
        /// </summary>
        public SoundSpecifier DrinkSound = new SoundPathSpecifier("/Audio/Items/drink.ogg");
    }
}
