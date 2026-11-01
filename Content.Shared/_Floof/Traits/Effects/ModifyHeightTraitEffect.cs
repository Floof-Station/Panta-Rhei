using Content.Shared._DV.Traits.Effects;
using Content.Shared._Floof.HeightAdjust;
//using Content.Server._Floof.HeightAdjust.Systems;
using Content.Shared.Humanoid;
using Content.Shared._DV.Humanoid;

namespace Content.Shared._Floof.Traits.Effects;

/// <summary>
/// Effect that changes a humanoid entity's height.
/// </summary>
public sealed partial class ModifyHeightEffect : BaseTraitEffect
{
    /// <summary>
    /// The factor to multiply height by.
    /// </summary>
    [DataField(required: true)]
    public float Factor = 1f;

    public override void Apply(TraitEffectContext ctx) // WIP, not ready for a live server!!
    {
        if (ctx.EntMan.TryGetComponent<HumanoidAppearanceComponent>(ctx.Player, out var humAppComp))
        {
            var oldHeight = humAppComp.Height;
            var newHeight = humAppComp.Height * Factor;
            
            SharedHumanoidAppearanceSystem.SetHeight(humAppComp, newHeight); // this doesn't seem to help with the primary issue: The HumanoidHeightMonitoringSystem and HeightChangedEvent are in Server.
            
            //humAppComp.Height = humAppComp.Height * Factor; // Need to make sure nothing is going to prevent height going out of species range, like I see in the client HumanoidAppearanceSystem UpdateSprite
            
            //RaiseLocalEvent(ctx.Player, new AppearanceLoadedEvent()); // Not sure it makes sense to call this, but do I need to in order to keep the HumanoidHeightMonitoringSystem in sync? Should I just update the associated component directly??
            //RaiseLocalEvent(ctx.Player, new HeightChangedEvent(oldScale, humAppComp.Height, Factor)); // HeightChangedEvent is in Server not shared. Is it possible for Shared to interact with server stuff?
            
            //ctx.EntMan.Dirty(ctx.Player, humAppComp); // Need to call an AppearanceLoadedEvent or something to integrate with the HumanoidHeightMonitoringSystem, or somehow handle that directly and instead update LastHeight and raise a HeightChangedEvent... but I'm not sure how to integrate with events here. Or I need to find a system/function to change height that exists elsewhere that already handles this (I don't think there is anything beyond Appearance Load/Profile Load or whatever it's called). Maybe I should add a SetHeight in the SharedHumanoidAppearanceSystem and handle the rest there? That might work better...  
        }
    }
}
