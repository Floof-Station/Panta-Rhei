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
            || !TryNormalizeSettings(ev.SettingsYaml, out var yaml, out var allowSound, out var digestSpeed))
        {
            _sawmill.Warning($"Rejected invalid Vore settings from {session.UserId}.");
            RaiseNetworkEvent(new VoreSettingsEvent { SettingsYaml = LoadSettings(session.UserId) }, session);
            return;
        }

        if (!SaveSettings(session.UserId, yaml))
            return;

        _settings[session.UserId] = yaml;
        RaiseNetworkEvent(new VoreSettingsEvent { SettingsYaml = yaml }, session);
        ApplyToAttachedPlayer(session, allowSound, digestSpeed);
    }

    private void OnPreyStartup(EntityUid uid, PreyComponent comp, ComponentStartup args)
    {
        if (!_playerManager.TryGetSessionByEntity(uid, out var session))
            return;

        comp.AllowSound = TryGetAllowSound(LoadSettings(session.UserId));
    }

    private void ApplyToAttachedPlayer(ICommonSession session, string yaml)
    {
        if (!TryNormalizeSettings(yaml, out _, out var allowSound, out var digestSpeed))
            return;

        ApplyToAttachedPlayer(session, allowSound, digestSpeed);
    }

    private void ApplyToAttachedPlayer(ICommonSession session, bool allowSound, float digestSpeed)
    {
        if (session.AttachedEntity is not { } uid)
            return;

        RaiseLocalEvent(uid, new VoreSettingsChangedEvent { 
            AllowSound = allowSound,
            DigestSpeed = digestSpeed
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
            if (!TryNormalizeSettings(yaml, out var normalized, out _, out _))
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

    private static bool TryNormalizeSettings(string yaml, out string normalized, out bool allowSound, out float digestSpeed)
    {
        normalized = string.Empty;
        allowSound = false;
        digestSpeed = 100f;

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
        return TryNormalizeSettings(yaml, out _, out var allowSound, out _) && allowSound;
    }

    private static ResPath GetSettingsPath(NetUserId userId)
    {
        return new ResPath($"/vore_settings_{userId.UserId:N}.yml");
    }
}