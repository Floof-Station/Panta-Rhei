using System.Numerics;
using Content.Shared._Floof.CCVar;
using Content.Shared.Administration;
using Content.Shared.Administration.Managers;
using Content.Shared.Camera;
using Content.Shared.CCVar;
using Content.Shared.Ghost;
using Content.Shared.Input;
using Content.Shared.Movement.Components;
using Robust.Shared.Configuration;
using Robust.Shared.Input.Binding;
using Robust.Shared.Player;
using Robust.Shared.Serialization;

namespace Content.Shared.Movement.Systems;

/// <summary>
/// Lets specific sessions scroll and set their zoom directly.
/// </summary>
public abstract class SharedContentEyeSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminManager _admin = default!;
    [Dependency] private readonly INetConfigurationManager _netConf = default!; // Floofstation
    [Dependency] private readonly ISharedPlayerManager _playerMan = default!; // Floofstation

    // Admin flags required to ignore normal eye restrictions.
    public const AdminFlags EyeFlag = AdminFlags.Debug;

    // Floofstation edit
    public static readonly Vector2 DefaultZoom = Vector2.One;
    public static Vector2 MinZoom { get; private set; } = DefaultZoom * (float)Math.Pow(1.5, -3); // Has to stay the same regardless of zoom mods
    public float ZoomInMod { get; private set; } = 1f;
    public float ZoomOutMod { get; private set; } = 1f;
    public int ZoomLevels { get; private set; } = 1;
    // Floofstation section end

    [Dependency] private readonly SharedEyeSystem _eye = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ContentEyeComponent, ComponentStartup>(OnContentEyeStartup);
        SubscribeAllEvent<RequestTargetZoomEvent>(OnContentZoomRequest);
        SubscribeAllEvent<RequestPvsScaleEvent>(OnPvsScale);
        SubscribeAllEvent<RequestEyeEvent>(OnRequestEye);

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.ZoomIn, InputCmdHandler.FromDelegate(ZoomIn, handle:false))
            .Bind(ContentKeyFunctions.ZoomOut, InputCmdHandler.FromDelegate(ZoomOut, handle:false))
            .Bind(ContentKeyFunctions.ResetZoom, InputCmdHandler.FromDelegate(ResetZoom, handle:false))
            .Register<SharedContentEyeSystem>();

        Log.Level = LogLevel.Info;
        UpdatesOutsidePrediction = true;
    }

    public override void Shutdown()
    {
        base.Shutdown();
        CommandBinds.Unregister<SharedContentEyeSystem>();
    }

    private void ResetZoom(ICommonSession? session)
    {
        if (TryComp(session?.AttachedEntity, out ContentEyeComponent? eye))
            ResetZoom(session.AttachedEntity.Value, eye);
    }

    private void ZoomOut(ICommonSession? session)
    {
        // Floofstation
        var channel = session?.Channel ?? _playerMan.LocalSession?.Channel;
        var mod = channel != null ? Math.Clamp(_netConf.GetClientCVar(channel, FloofCCVars.ZoomOutStep), 1.05f, 2f) : 1.2f;

        if (TryComp(session?.AttachedEntity, out ContentEyeComponent? eye))
            SetZoom(session.AttachedEntity.Value, eye.TargetZoom * mod, eye: eye);
    }

    private void ZoomIn(ICommonSession? session)
    {
        // Floofstation
        var channel = session?.Channel ?? _playerMan.LocalSession?.Channel;
        var mod = channel != null ? Math.Clamp(_netConf.GetClientCVar(channel, FloofCCVars.ZoomInStep), 0.25f, 0.97f) : 1.2f;

        if (TryComp(session?.AttachedEntity, out ContentEyeComponent? eye))
            SetZoom(session.AttachedEntity.Value, eye.TargetZoom * mod, eye: eye);
    }

    private Vector2 Clamp(Vector2 zoom, ContentEyeComponent component)
    {
        return Vector2.Clamp(zoom, MinZoom, component.MaxZoom);
    }

    /// <summary>
    /// Sets the target zoom, optionally ignoring normal zoom limits.
    /// </summary>
    public void SetZoom(EntityUid uid, Vector2 zoom, bool ignoreLimits = false, ContentEyeComponent? eye = null)
    {
        if (!Resolve(uid, ref eye, false))
            return;

        eye.TargetZoom = ignoreLimits ? zoom : Clamp(zoom, eye);
        Dirty(uid, eye);
    }

    private void OnContentZoomRequest(RequestTargetZoomEvent msg, EntitySessionEventArgs args)
    {
        var ignoreLimit = msg.IgnoreLimit && _admin.HasAdminFlag(args.SenderSession, EyeFlag);

        if (TryComp<ContentEyeComponent>(args.SenderSession.AttachedEntity, out var content))
            SetZoom(args.SenderSession.AttachedEntity.Value, msg.TargetZoom, ignoreLimit, eye: content);
    }

    private void OnPvsScale(RequestPvsScaleEvent ev, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is {} uid && _admin.HasAdminFlag(args.SenderSession, EyeFlag))
            _eye.SetPvsScale(uid, ev.Scale);
    }

    private void OnRequestEye(RequestEyeEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } player)
            return;

        if (!HasComp<GhostComponent>(player) && !_admin.IsAdmin(player))
            return;

        if (TryComp<EyeComponent>(player, out var eyeComp))
        {
            _eye.SetDrawFov(player, msg.DrawFov, eyeComp);
            _eye.SetDrawLight((player, eyeComp), msg.DrawLight);
        }
    }

    private void OnContentEyeStartup(EntityUid uid, ContentEyeComponent component, ComponentStartup args)
    {
        if (!TryComp<EyeComponent>(uid, out var eyeComp))
            return;

        _eye.SetZoom(uid, component.TargetZoom, eyeComp);
        Dirty(uid, component);
    }

    public void ResetZoom(EntityUid uid, ContentEyeComponent? component = null)
    {
        _eye.SetPvsScale(uid, 1);
        SetZoom(uid, DefaultZoom, eye: component);
    }

    public void SetMaxZoom(EntityUid uid, Vector2 value, ContentEyeComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.MaxZoom = value;
        component.TargetZoom = Clamp(component.TargetZoom, component);
        Dirty(uid, component);
    }

    public void UpdateEyeOffset(Entity<EyeComponent> eye)
    {
        var evAttempt = new GetEyeOffsetAttemptEvent();
        RaiseLocalEvent(eye, ref evAttempt);

        if (evAttempt.Cancelled)
        {
            _eye.SetOffset(eye, Vector2.Zero, eye);
            return;
        }

        var ev = new GetEyeOffsetEvent();
        RaiseLocalEvent(eye, ref ev);

        var evRelayed = new GetEyeOffsetRelayedEvent();
        RaiseLocalEvent(eye, ref evRelayed);

        _eye.SetOffset(eye, ev.Offset + evRelayed.Offset, eye);
    }

    public void UpdatePvsScale(EntityUid uid, ContentEyeComponent? contentEye = null, EyeComponent? eye = null)
    {
        if (!Resolve(uid, ref contentEye) || !Resolve(uid, ref eye))
            return;

        var evAttempt = new GetEyePvsScaleAttemptEvent();
        RaiseLocalEvent(uid, ref evAttempt);

        if (evAttempt.Cancelled)
        {
            _eye.SetPvsScale((uid, eye), 1);
            return;
        }

        var ev = new GetEyePvsScaleEvent();
        RaiseLocalEvent(uid, ref ev);

        var evRelayed = new GetEyePvsScaleRelayedEvent();
        RaiseLocalEvent(uid, ref evRelayed);

        _eye.SetPvsScale((uid, eye), 1 + ev.Scale + evRelayed.Scale);
    }

    /// <summary>
    /// Sendable from client to server to request a target zoom.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class RequestTargetZoomEvent : EntityEventArgs
    {
        public Vector2 TargetZoom;
        public bool IgnoreLimit;
    }

    /// <summary>
    /// Client->Server request for new PVS scale.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class RequestPvsScaleEvent(float scale) : EntityEventArgs
    {
        public float Scale = scale;
    }

    /// <summary>
    /// Sendable from client to server to request changing fov.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class RequestEyeEvent : EntityEventArgs
    {
        public readonly bool DrawFov;
        public readonly bool DrawLight;

        public RequestEyeEvent(bool drawFov, bool drawLight)
        {
            DrawFov = drawFov;
            DrawLight = drawLight;
        }
    }
}
