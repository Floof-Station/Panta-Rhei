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
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    private static readonly Vector3 StomachWallColor = new(0.35f, 0.01f, 0.06f);
    private static readonly Color AmbientDimColor = new(0.0f, 0.0f, 0.0f, 0.65f);
    private static readonly ProtoId<ShaderPrototype> CircleMaskShader = "GradientCircleMask";
    
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

    /// <summary>
    /// dimming and color overlay to simulate being inside a stomach
    /// </summary>
    protected override void Draw(in OverlayDrawArgs args){
        var worldHandle = args.WorldHandle;
        var viewport = args.WorldAABB;
        var viewWidth = args.ViewportBounds.Width;
        var time = (float) _timing.RealTime.TotalSeconds;
        /* TODO REPLACE later with digestcomponent values to indicate digestion process by walls closing in*/
        float tempdigest = 1f; 

        // defining the stomach walls
        float outerScale = 1.0f - (tempdigest * 0.6f);
        float innerScale = 0.25f - (tempdigest * 0.25f);
        var outerRadius = outerScale * viewWidth;
        var innerRadius = innerScale * viewWidth;
        var outerCircleMaxRadius = outerRadius + (0.13f * viewWidth);
        var innerCircleMaxRadius = innerRadius + (0.03f * viewWidth);

        // simulating pulses of circle movement and coloring
        var pulsing = MathF.Cos(time * 0.5f - 1.5f) + 1f;
        _stomachShader.SetParameter("time", pulsing);
        _stomachShader.SetParameter("color", StomachWallColor); 
        _stomachShader.SetParameter("darknessAlphaOuter", 0.99f);

        // drawing of the actual circles
        _stomachShader.SetParameter("outerCircleRadius", outerRadius);
        _stomachShader.SetParameter("outerCircleMaxRadius", outerCircleMaxRadius);
        _stomachShader.SetParameter("innerCircleRadius", innerRadius);
        _stomachShader.SetParameter("innerCircleMaxRadius", innerCircleMaxRadius);

        // drawing the shader
        worldHandle.UseShader(_stomachShader);
        worldHandle.DrawRect(viewport, Color.White); 
        worldHandle.UseShader(null);

        // dimming the screen lightly
        worldHandle.DrawRect(viewport, AmbientDimColor);
    }    
}