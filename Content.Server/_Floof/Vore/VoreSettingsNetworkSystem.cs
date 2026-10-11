using System.Globalization;
using System.IO;
using Content.Shared._Floof.Vore;
using Robust.Server.Player;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Server._Floof.Vore;

public sealed class VoreSettingsNetworkSystem : EntitySystem
{
    private const int MaxSettingsLength = 65536;

    private readonly record struct SettingsValues(bool AllowSound, float DigestSpeed, Color StomachColor);

    [Dependency] private readonly IResourceManager _resources = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    private readonly Dictionary<NetUserId, string> _settings = new();
    private ISawmill _sawmill = default!;

    public override void Initialize()
    {
        _sawmill = Logger.GetSawmill("vore.settings");
        SubscribeNetworkEvent<VoreSettingsRequestEvent>(OnSettingsRequested);
        SubscribeNetworkEvent<VoreSettingsEvent>(OnSettingsChanged);
        SubscribeLocalEvent<PreyComponent, ComponentStartup>(OnPreyStartup);
    }

    private void OnSettingsRequested(VoreSettingsRequestEvent ev, EntitySessionEventArgs args)
    {
        if (args.SenderSession is not { } session)
            return;

        var yaml = LoadSettings(session.UserId);
        RaiseNetworkEvent(new VoreSettingsEvent { SettingsYaml = yaml }, session);
        if (TryNormalizeSettings(yaml, out _, out var settings))
            ApplyToAttachedPlayer(session, settings);
    }

    private void OnSettingsChanged(VoreSettingsEvent ev, EntitySessionEventArgs args)
    {
        if (args.SenderSession is not { } session)
            return;

        if (ev.SettingsYaml.Length > MaxSettingsLength
            || !TryNormalizeSettings(ev.SettingsYaml, out var yaml, out var settings))
        {
            _sawmill.Warning($"Rejected invalid Vore settings from {session.UserId}.");
            RaiseNetworkEvent(new VoreSettingsEvent { SettingsYaml = LoadSettings(session.UserId) }, session);
            return;
        }

        if (!SaveSettings(session.UserId, yaml))
            return;

        _settings[session.UserId] = yaml;
        RaiseNetworkEvent(new VoreSettingsEvent { SettingsYaml = yaml }, session);
        ApplyToAttachedPlayer(session, settings);
    }

    //TODO REPLACE THIS REDUNDANCY
    private void OnPreyStartup(EntityUid uid, PreyComponent comp, ComponentStartup args)
    {
        if (!_playerManager.TryGetSessionByEntity(uid, out var session))
            return;

        comp.AllowSound = TryNormalizeSettings(LoadSettings(session.UserId), out _, out var settings)
            && settings.AllowSound;
    }

    /// <summary>
    /// applies settings for the other systems to use such as their components
    /// </summary>
    private void ApplyToAttachedPlayer(ICommonSession session, SettingsValues settings)
    {
        if (session.AttachedEntity is not { } uid)
            return;

        RaiseLocalEvent(uid, new VoreSettingsChangedEvent { 
            AllowSound = settings.AllowSound,
            DigestSpeed = settings.DigestSpeed,
            StomachColor = settings.StomachColor,
            });
    }

    private string LoadSettings(NetUserId userId)
    {
        if (_settings.TryGetValue(userId, out var cached))
            return cached;

        var path = GetSettingsPath(userId);
        if (!_resources.UserData.Exists(path))
            return _settings[userId] = string.Empty;

        try
        {
            var yaml = _resources.UserData.ReadAllText(path);
            if (!TryNormalizeSettings(yaml, out var normalized, out _))
                throw new InvalidDataException("Settings file is not a valid mapping.");

            return _settings[userId] = normalized;
        }
        catch (Exception e)
        {
            _sawmill.Error($"Failed to load Vore settings for {userId}: {e}");
            return _settings[userId] = string.Empty;
        }
    }

    private bool SaveSettings(NetUserId userId, string yaml)
    {
        try
        {
            _resources.UserData.WriteAllText(GetSettingsPath(userId), yaml);
            return true;
        }
        catch (Exception e)
        {
            _sawmill.Error($"Failed to save Vore settings for {userId}: {e}");
            return false;
        }
    }

    ///<summary>
    /// turning the input into a consistent form
    /// </summary>
    private static bool TryNormalizeSettings(string yaml, out string normalized, out SettingsValues settings){
        normalized = string.Empty;
        var defaults = new PredComponent();
        settings = new SettingsValues(false, defaults.DigestSpeed, defaults.StomachColor);

        try
        {
            using var reader = new StringReader(yaml);
            var stream = new YamlStream();
            stream.Load(reader);
            if (stream.Documents.Count != 1 || stream.Documents[0].RootNode.ToDataNode() is not MappingDataNode mapping)
                return false;

            var allowSound = false;
            if (mapping.TryGet("allowSound", out var soundNode)
                && soundNode is ValueDataNode soundValue
                && !bool.TryParse(soundValue.Value, out allowSound))
            {
                return false;
            }

            var digestSpeed = defaults.DigestSpeed;
            if (mapping.TryGet("digestSpeed", out var speedNode))
            {
                if (speedNode is not ValueDataNode speedValue
                    || !float.TryParse(speedValue.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out digestSpeed)
                    || !float.IsFinite(digestSpeed))
                {
                    return false;
                }

                digestSpeed = Math.Clamp(digestSpeed, 10f, 300f);
            }

            var stomachColor = defaults.StomachColor;
            if (mapping.TryGet("stomachColor", out var colorNode))
            {
                if (colorNode is not ValueDataNode colorValue
                    || Color.TryFromHex(colorValue.Value) is not { } parsedColor)
                {
                    return false;
                }

                stomachColor = parsedColor;
            }

            using var writer = new StringWriter();
            mapping.Write(writer);
            normalized = writer.ToString();
            settings = new SettingsValues(allowSound, digestSpeed, stomachColor);
            return true;
        }
        catch
        {
            return false;
        }
    }

    ///<summary>
    /// the file path for the vore settings
    /// </summary>
    private static ResPath GetSettingsPath(NetUserId userId)
    {
        return new ResPath($"/vore_settings_{userId.UserId:N}.yml");
    }
}