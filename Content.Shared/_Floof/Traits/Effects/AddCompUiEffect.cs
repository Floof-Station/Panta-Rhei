using Content.Shared._DV.Traits.Effects;

namespace Content.Shared._Floof.Traits.Effects;

/// <summary>
/// Adds a UserInterface to the player.
/// This is used for components like HarpySinger where the UI is initialized in the most fishy way possible,
/// So we need to create a fishy system like this to get the ui to work with AddCompEffect.
/// </summary>
public sealed partial class AddCompUiEffect : BaseTraitEffect
{
    [DataField(required: true)]
    public Dictionary<Enum, InterfaceData> Interfaces = new();

    public override void Apply(TraitEffectContext ctx)
    {
        foreach (var (key, data) in Interfaces)
        {
            ctx.EntMan.System<SharedUserInterfaceSystem>().SetUi(ctx.Player, key, data);
        }
    }
}
