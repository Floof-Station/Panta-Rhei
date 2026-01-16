using System.Diagnostics.CodeAnalysis;
using Content.Client._Floof.Language.Systems;
using Content.Shared._Floof.Language;
using Content.Shared._Floof.Language.Systems;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.RichText;
using Robust.Shared.Input;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client._Floof.Language.RichText;

/// <summary>
/// Injects text that can only be seen in its original form if the local player has a LanguageSpeakerComponent and can understand the specified language.
/// </summary>
/// <example>The following text is written in Canilunzt: [language="Canilunzt" text="Hello, world!"/]</example>
[UsedImplicitly]
public sealed class LanguageMarkupTag : IMarkupTagHandler
{
    public string Name => "language";
    public static readonly string Example = "[language=\"Canilunzt\" text=\"Hello, world!\"/]";

    // Show a label that, when hovered over, shows a tooltip with the language name and whether the player knows it.
    public bool TryCreateControl(MarkupNode node, [NotNullWhen(true)] out Control? control)
    {
        var label = new Label();
        control = label;

        // Error messages are handled by BeforeText
        if (!node.Value.TryGetString(out var name) || !node.Attributes.TryGetValue("text", out var textAttr) || !textAttr.TryGetString(out var text))
            return false;

        var protoMan = IoCManager.Resolve<IPrototypeManager>();
        if (!protoMan.TryIndex(name, out LanguagePrototype? languageProto))
            return false;

        var knowsLanguage = false;
        if (IoCManager.Resolve<IEntitySystemManager>().TryGetEntitySystem<LanguageSystem>(out var languageSys))
            knowsLanguage = languageSys.CanLocalPlayerUnderstand(languageProto.ID);

        label.Text = "[lg]";
        label.MouseFilter = Control.MouseFilterMode.Stop;
        label.DefaultCursorShape = Control.CursorShape.Hand;
        // Color the text with the language's name and show a popup with the language name when hovered
        var defColor = Color.DarkGray;
        label.FontColorOverride = defColor;
        label.OnMouseEntered += _ => label.FontColorOverride = languageProto.SpeechOverride.Color ?? defColor;
        label.OnMouseExited += _ => label.FontColorOverride = defColor;

        label.TooltipSupplier = _ =>
        {
            var t = new Tooltip();
            t.Text = $"This text is written in {languageProto.Name}. You {(knowsLanguage ? "know this language" : "don't know this language")}.\n";
            if (!knowsLanguage)
                return t;

            t.Text += "Right click to show the obfuscated version.";
            t.MouseFilter = Control.MouseFilterMode.Pass;
            // Quite counterintuitively, we add the key handler to the label, not the tooltip. This is because the tooltip is hard to click.
            label.OnKeyBindDown += args =>
            {
                if (args.Function != EngineKeyFunctions.UIRightClick || languageSys == null)
                    return;

                t.Text = languageSys.ObfuscateSpeech(text, languageProto);
            };

            return t;
        };
        label.TooltipDelay = 0.3f;

        return true;
    }

    // We want to render the value of this tag as text. Errors are written in all-caps to hopefully throw the reader off and prevent them from thinking it's IC text.
    public string TextBefore(MarkupNode node)
    {
        if (!node.Value.TryGetString(out var languageId))
            return $"[ERROR: SPECIFY A LANGUAGE ID. EXAMPLE: {Example} ]";

        if (!node.Attributes.TryGetValue("text", out var textAttr) || !textAttr.TryGetString(out var text))
            return $"[ERROR: SPECIFY A TEXT ATTRIBUTE. EXAMPLE: {Example} ]";

        var protoMan = IoCManager.Resolve<IPrototypeManager>();
        if (!protoMan.TryIndex<LanguagePrototype>(languageId, out var language))
            return $"[ERROR: LANGUAGE WITH ID {languageId} DOES NOT EXIST. CONSULT THE /languagelist COMMAND.]";

        if (!IoCManager.Resolve<IEntitySystemManager>().TryGetEntitySystem<LanguageSystem>(out var languageSys))
            return "[ERROR: LANGUAGE SYSTEM HAS NOT BEEN LOADED. THIS IS A BUG.]";

        // Not checking for RequireSpeech because we may have written-only languages later
        if (language.SpeechOverride is not { RequireHands: false, AllowWriting: true })
            return "[ERROR: THIS LANGUAGE CANNOT BE WRITTEN]";

        return languageSys.CanLocalPlayerUnderstand(language) ? text : languageSys.ObfuscateSpeech(text, language);
    }
}
