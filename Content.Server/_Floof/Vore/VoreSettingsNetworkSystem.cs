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
        ApplyToAttachedPlayer(session, yaml);
    }

    private void OnSettingsChanged(VoreSettingsEvent ev, EntitySessionEventArgs args)
    {
        if (args.SenderSession is not { } session)
            return;

        if (ev.SettingsYaml.Length > MaxSettingsLength
            || !TryNormalizeSettings(ev.SettingsYaml, out var yaml, out var allowSound, out var digestSpeed, out var stomachColor))
        {
            _sawmill.Warning($"Rejected invalid Vore settings from {session.UserId}.");
            RaiseNetworkEvent(new VoreSettingsEvent { SettingsYaml = LoadSettings(session.UserId) }, session);
            return;
        }

        if (!SaveSettings(session.UserId, yaml))
            return;

        _settings[session.UserId] = yaml;
        RaiseNetworkEvent(new VoreSettingsEvent { SettingsYaml = yaml }, session);
        ApplyToAttachedPlayer(session, allowSound, digestSpeed, stomachColor);
    }

    private void OnPreyStartup(EntityUid uid, PreyComponent comp, ComponentStartup args)
    {
        if (!_playerManager.TryGetSessionByEntity(uid, out var session))
            return;

        comp.AllowSound = TryGetAllowSound(LoadSettings(session.UserId));
    }

    private void ApplyToAttachedPlayer(ICommonSession session, string yaml)
    {
        if (!TryNormalizeSettings(yaml, out _, out var allowSound, out var digestSpeed, out var stomachColor))
            return;

        ApplyToAttachedPlayer(session, allowSound, digestSpeed, stomachColor);
    }

    private void ApplyToAttachedPlayer(ICommonSession session, bool allowSound, float digestSpeed, Color stomachColor)
    {
        if (session.AttachedEntity is not { } uid)
            return;

        RaiseLocalEvent(uid, new VoreSettingsChangedEvent { 
            AllowSound = allowSound,
            DigestSpeed = digestSpeed,
            StomachColor = stomachColor,
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
            if (!TryNormalizeSettings(yaml, out var normalized, out _, out _, out _))
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

    private static bool TryNormalizeSettings(
        string yaml,
        out string normalized,
        out bool allowSound,
        out float digestSpeed,
        out Color stomachColor)
    {
        normalized = string.Empty;
        allowSound = false;
        digestSpeed = 100f;
        stomachColor = new(0.35f, 0.01f, 0.06f);

        try
        {
            using var reader = new StringReader(yaml);
            var stream = new YamlStream();
            stream.Load(reader);
            if (stream.Documents.Count != 1 || stream.Documents[0].RootNode.ToDataNode() is not MappingDataNode mapping)
                return false;

            if (mapping.TryGet("allowSound", out var soundNode)
                && soundNode is ValueDataNode soundValue
                && !bool.TryParse(soundValue.Value, out allowSound))
            {
                return false;
            }

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
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryGetAllowSound(string yaml)
    {
        return TryNormalizeSettings(yaml, out _, out var allowSound, out _, out _) && allowSound;
    }

    private static ResPath GetSettingsPath(NetUserId userId)
    {
        return new ResPath($"/vore_settings_{userId.UserId:N}.yml");
    }
}