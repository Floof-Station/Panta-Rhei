using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.Inventory;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server._Floof.Vampire;

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

    /// <summary>
    ///     Damage to deal when sucking. When damage in these groups becomes zero, also removes the flavor text.
    /// </summary>
    public DamageSpecifier BiteDamage = new DamageSpecifier()
    {
        DamageDict = new()
        {
            { "Piercing", 5 },
            { "Airloss", 0 }, // This is just so that BloodSuckedComponent doesn't get removed until bloodloss reaches 0
        },
    };

    public SoundSpecifier BiteSound = new SoundPathSpecifier("/Audio/Effects/bite.ogg");

    /// <summary>
    ///     Which slots to consider when checking mouth obstruction.
    /// </summary>
    public SlotFlags RequiredFreeSlot = SlotFlags.MASK;

    // ***INJECT WHEN SUCC***

    /// <summary>
    /// Whether to inject chems into a chemstream when we suck something.
    /// </summary>
    [DataField("injectWhenSucc")]
    public bool InjectWhenSucc = false;

    /// <summary>
    /// How many units of our injected chem to inject.
    /// </summary>
    [DataField("unitsToInject")]
    public float UnitsToInject = 5;

    /// <summary>
    /// Which reagent to inject.
    /// </summary>
    [DataField("injectReagent")]
    public string InjectReagent = "";

    /// <summary>
    /// Whether we need to web the thing up first...
    /// </summary>
    [DataField("webRequired")]
    public bool WebRequired = false;
}
