using System.Diagnostics.CodeAnalysis;
using Content.Client._Floof.Language.Systems;
using Content.Shared._Floof.CCVar;
using Content.Shared._Floof.Language;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.RichText;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client._Floof.Language.RichText;

[UsedImplicitly]
public sealed class LanguageIconTag : IMarkupTagHandler
{
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IEntitySystemManager _sysMan = default!;

    public const string TagName = "langicon";
    public string Name => TagName;

    public bool TryCreateControl(MarkupNode node, [NotNullWhen(true)] out Control? control)
    {
        if (!node.Value.TryGetString(out var languageId)
            || !_protoMan.TryIndex<LanguagePrototype>(languageId, out var language)
            || language.Icon is null
            || !language.IsVisibleLanguage)
        {
            control = null;
            return false;
        }

        var tex = new TextureRect()
        {
            Stretch = TextureRect.StretchMode.Scale,
            CanShrink = true,
            SetSize = new(16, 16),
        };

        var spriteSys = _sysMan.GetEntitySystem<SpriteSystem>();
        tex.Texture = spriteSys.Frame0(language.Icon);

        tex.TooltipSupplier = _ =>
        {
            var langSys = _sysMan.GetEntitySystem<LanguageSystem>();
            var understands = langSys.CanLocalPlayerUnderstand(language);
            var name = language.Name;

            var msg = $"This text is written in {name}. You {(understands ? "understand" : "don't understand")} it.";
            return new Tooltip() { Text = msg };
        };

        control = tex;
        return true;
    }

    public string TextBefore(MarkupNode node)
    {
        if (!TryGetLanguageProto(node, out var language) || !language.IsVisibleLanguage)
            return "";

        return "[";
    }

    public string TextAfter(MarkupNode node)
    {
        if (!TryGetLanguageProto(node, out var language) || !language.IsVisibleLanguage)
            return "";

        // If the local player turned on the "display language names" setting, show the language name in brackets
        if (!_cfg.GetCVar(FloofCCVars.AccessibilityFullLanguageNames))
            return "]";

        return $"{language.Name}]";
    }

    private bool TryGetLanguageProto(MarkupNode node, [NotNullWhen(true)] out LanguagePrototype? language)
    {
        if (!node.Value.TryGetString(out var languageId))
        {
            language = null;
            return false;
        }

        return _protoMan.TryIndex(languageId, out language);
    }
}
