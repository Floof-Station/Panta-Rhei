using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared._Euphoria.Selector.Events;
using Content.Shared.Examine;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Network;
using Robust.Shared.Utility;

namespace Content.Shared._Euphoria.Selector;

public sealed class EntityConfigurationSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popups = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<EntityConfigurationComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<EntityConfigurationComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<EntityConfigurationComponent, GetVerbsEvent<Verb>>(OnGetVerbs);
    }

    private void OnMapInit(Entity<EntityConfigurationComponent> ent, ref MapInitEvent args)
    {
        foreach (var (id, configGroup) in ent.Comp.ConfigGroups)
        {
            DebugTools.Assert(configGroup.Options.Length > 0);

            // Assign internal IDs just in case. Not sure if ill need it.
            configGroup.Id = id;

            // For each config group, we select the first value as the current config if it's absent or invalid
            if (!ent.Comp.CurrentConfig.TryGetValue(id, out var currentValue)
                || configGroup.Options.All(it => it.Value != currentValue))
            {
                ent.Comp.CurrentConfig[id] = configGroup.Options[0].Value;
            }

            // Raise relevant events
            TrySelect(ent!, null, id, ent.Comp.CurrentConfig[id], force: true, silent: true);
        }
    }

    private void OnExamined(Entity<EntityConfigurationComponent> ent, ref ExaminedEvent args)
    {
        if (!ent.Comp.ShowExamine)
            return;

        using (args.PushGroup(nameof(EntityConfigurationComponent), -1))
        {
            args.PushMarkup(Loc.GetString("verb-selector-config-header", ("target", ent.Owner)));

            foreach (var (groupId, value) in ent.Comp.CurrentConfig)
            {
                var group = ent.Comp.ConfigGroups[groupId];
                var groupName = Loc.GetString(group.ExamineName ?? group.Name, ("value", value));

                args.PushMarkup($"- {groupName}"); // We let the group name loc string handle showing the value
            }
        }
    }

    private void OnGetVerbs(Entity<EntityConfigurationComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        var user = args.User;
        if (!args.CanAccess || !args.CanInteract || !args.CanComplexInteract)
            return;

        foreach (var (groupId, configGroup) in ent.Comp.ConfigGroups)
        {
            var currentValue = ent.Comp.CurrentConfig[groupId];
            var textCat = Loc.GetString(configGroup.Name, ("value", currentValue));
            var verbCat = new VerbCategory(textCat, configGroup.Icon, resolveLoc: false); // meow

            int index = 0;
            foreach (var option in configGroup.Options)
            {
                var message = Loc.GetString(option.Name, ("value", option.Value));
                var verb = new Verb()
                {
                    Category = verbCat,
                    CloseMenu = true,
                    DoContactInteraction = true,
                    Priority = -(index++), // Preserve the order of verbs as defined in yaml
                    Text = message,
                    Act = () => TrySelect(ent!, user, groupId, option.Value),
                };

                args.Verbs.Add(verb);
            }
        }
    }

    public bool TrySelect(Entity<EntityConfigurationComponent?> ent, EntityUid? user, string groupId, string newValue, bool force = false, bool silent = false)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        // Sanity check
        if (!ent.Comp.ConfigGroups.TryGetValue(groupId, out var configGroup)
            || (!force && configGroup.Options.All(it => it.Value != newValue)))
            return false;

        if (!force)
        {
            var beforeEv = new BeforeConfigurationSelectedEvent(configGroup, newValue, ent.Comp.CurrentConfig, user);
            RaiseLocalEvent(ent, beforeEv);

            if (beforeEv.Cancelled)
            {
                if (beforeEv.CancelReason != null && _net.IsServer)
                    _popups.PopupEntity(beforeEv.CancelReason, ent);
                return false;
            }
        }

        ent.Comp.CurrentConfig[groupId] = newValue;
        Dirty(ent);

        var afterEv = new ConfigurationSelectedEvent(configGroup, newValue, ent.Comp.CurrentConfig, user);
        RaiseLocalEvent(ent, afterEv);

        if (_net.IsServer && !silent)
        {
            _popups.PopupEntity(Loc.GetString(configGroup.SelectedPopup, ("groupId", groupId), ("value", newValue)), ent);
        }

        return true;
    }

    public bool TryGetConfig(Entity<EntityConfigurationComponent?> ent, string groupId, [NotNullWhen(true)] out string? value)
    {
        if (!Resolve(ent, ref ent.Comp))
        {
            value = null;
            return false;
        }

        return ent.Comp.CurrentConfig.TryGetValue(groupId, out value);
    }

    public bool TryGetConfigInt(Entity<EntityConfigurationComponent?> ent, string groupId, out int value)
    {
        if (!TryGetConfig(ent, groupId, out var strValue))
        {
            value = 0;
            return false;
        }

        value = int.Parse(strValue);
        return true;
    }

    public bool TryGetConfigFloat(Entity<EntityConfigurationComponent?> ent, string groupId, out float value)
    {
        if (!TryGetConfig(ent, groupId, out var strValue))
        {
            value = 0;
            return false;
        }

        value = float.Parse(strValue);
        return true;
    }
}
