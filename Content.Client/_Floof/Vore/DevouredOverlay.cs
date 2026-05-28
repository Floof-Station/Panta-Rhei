using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Content.Shared._Floof.Vore;

namespace Content.Client._Floof.Vore;

public sealed class DevouredOverlay : Overlay
{
    private static readonly ProtoId<ShaderPrototype> CircleMaskShader = "GradientCircleMask";

    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    private readonly ShaderInstance _stomachShader;
    

    public DevouredOverlay()
    {
        IoCManager.InjectDependencies(this);
        _stomachShader = _prototypeManager.Index(CircleMaskShader).InstanceUnique();
    }

    /// <summary>
    /// making sure the right entity is attached and has the necessary components
    /// </summary>
    protected override bool BeforeDraw(in OverlayDrawArgs args){
        var playerEntity = _playerManager.LocalSession?.AttachedEntity;
        if (playerEntity == null)
            return false;
        if (!_entityManager.TryGetComponent(playerEntity, out EyeComponent? eyeComp) || args.Viewport.Eye != eyeComp.Eye)
            return false;
        if (!_entityManager.HasComponent<DevouredComponent>(playerEntity.Value))
            return false;

        return true;
    }

    protected override void Draw(in OverlayDrawArgs args){
        var worldHandle = args.WorldHandle;
        var viewport = args.WorldAABB;
        var distance = args.ViewportBounds.Width;

        var time = (float) _timing.RealTime.TotalSeconds;
        var lastFrameTime = (float) _timing.FrameTime.TotalSeconds;

        // defining the stomach walls
        float outerMaxLevel = 1.3f * distance;
        float outerMinLevel = 0.4f * distance;
        float innerMaxLevel = 0.4f * distance;
        float innerMinLevel = 0.05f * distance;
        // TODO REPLACE later with digestcomponent values to indicate digestion process visually
        float tempdigest = 0.5f; 

        var outerRadius = outerMaxLevel -  tempdigest * (outerMaxLevel - outerMinLevel);
        var innerRadius = innerMaxLevel -  tempdigest * (innerMaxLevel - innerMinLevel);

        // simulating pulses of circle movement
        var breath = MathF.Sin(time * 0.9f);
        var organicContraction = MathF.Pow(MathF.Max(0f, breath), 2.5f); 

        _stomachShader.SetParameter("time", organicContraction);
        _stomachShader.SetParameter("color", new Vector3(0.35f, 0.01f, 0.06f)); 
        _stomachShader.SetParameter("darknessAlphaOuter", 0.99f); 

        // drawing of the actual circles
        _stomachShader.SetParameter("outerCircleRadius", outerRadius);
        _stomachShader.SetParameter("outerCircleMaxRadius", outerRadius + 0.20f * distance);
        _stomachShader.SetParameter("innerCircleRadius", innerRadius);
        _stomachShader.SetParameter("innerCircleMaxRadius", innerRadius + 0.04f * distance);

        worldHandle.UseShader(_stomachShader);
        worldHandle.DrawRect(viewport, Color.White); 
        worldHandle.UseShader(null);

        // dimming the screen lightly
        var screenDimmer = new Color(0.0f, 0.0f, 0.0f, 0.65f); 
        worldHandle.DrawRect(viewport, screenDimmer);
    }    
}