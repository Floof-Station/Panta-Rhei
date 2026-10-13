using Content.Shared._DV.Traits.Effects;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Humanoid;
using Content.Shared.Speech;
using Content.Shared.Speech.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared._Euphoria.Traits.Effects;

public sealed partial class EmoteSoundsEffect : BaseTraitEffect
{
    [DataField]
    public Dictionary<Sex, ProtoId<EmoteSoundsPrototype>> Sounds = new();

    public override void Apply(TraitEffectContext ctx)
    {
        if (!ctx.EntMan.TryGetComponent<VocalComponent>(ctx.Player, out var vocal))
            return;

        vocal.Sounds = Sounds;
        ctx.EntMan.Dirty(ctx.Player, vocal);

        var ev = new SoundsChangedEvent();
        ctx.EntMan.EventBus.RaiseLocalEvent(ctx.Player, ref ev);
    }
}
