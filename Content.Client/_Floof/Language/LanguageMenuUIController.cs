using System.Linq;
using Content.Client._Floof.Language.Systems;
using Content.Client.Gameplay;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Systems.MenuBar.Widgets;
using Content.Shared._Floof.Language;
using Content.Shared.Input;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input.Binding;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using static Robust.Client.UserInterface.Controls.BaseButton;

namespace Content.Client._Floof.Language;

[UsedImplicitly]
public sealed class LanguageMenuUIController : UIController, IOnStateEntered<GameplayState>, IOnStateExited<GameplayState>
{
    [UISystemDependency] private readonly LanguageSystem? _languages = default!;

    public LanguageMenuWindow? LanguageWindow;
    private MenuButton? LanguageButton => UIManager.GetActiveUIWidgetOrNull<GameTopMenuBar>()?.LanguageButton;

    public void OnStateEntered(GameplayState state)
    {
        DebugTools.Assert(LanguageWindow == null);

        LanguageWindow = UIManager.CreateWindow<LanguageMenuWindow>();
        LayoutContainer.SetAnchorPreset(LanguageWindow, LayoutContainer.LayoutPreset.CenterTop);
        LanguageWindow.ClientLanguageSystem = _languages;

        LanguageWindow.OnClose += () =>
        {
            if (LanguageButton != null)
                LanguageButton.Pressed = false;
        };
        LanguageWindow.OnOpen += () =>
        {
            if (LanguageButton != null)
                LanguageButton.Pressed = true;
        };
        LanguageWindow.OnLanguageChosen += OnLanguageChosen;
        LanguageWindow.OnLanguageMoveUp += language => MoveLanguage(language, -1);
        LanguageWindow.OnLanguageMoveDown += language => MoveLanguage(language, 1);

        _languages?.OnLanguagesChanged += UpdateWindowState;

        CommandBinds.Builder.Bind(ContentKeyFunctions.OpenLanguageMenu,
            InputCmdHandler.FromDelegate(_ => ToggleWindow())).Register<LanguageMenuUIController>();
    }

    public void OnStateExited(GameplayState state)
    {
        if (LanguageWindow != null)
        {
            LanguageWindow.Dispose();
            LanguageWindow = null;
        }

        _languages?.OnLanguagesChanged -= UpdateWindowState;

        CommandBinds.Unregister<LanguageMenuUIController>();
    }

    public void UnloadButton()
    {
        if (LanguageButton == null)
            return;

        LanguageButton.OnPressed -= LanguageButtonPressed;
    }

    public void LoadButton()
    {
        if (LanguageButton == null)
            return;

        LanguageButton.OnPressed += LanguageButtonPressed;
    }

    private void LanguageButtonPressed(ButtonEventArgs args)
    {
        ToggleWindow();
    }

    private void ToggleWindow()
    {
        if (LanguageWindow == null)
            return;

        if (LanguageButton != null)
            LanguageButton.SetClickPressed(!LanguageWindow.IsOpen);

        if (!LanguageWindow.IsOpen)
        {
            UpdateWindowState();
            LanguageWindow.Open();
        }
        else
            LanguageWindow.Close();
    }

    private void UpdateWindowState()
    {
        var languageSpeaker = _languages?.GetLocalSpeaker();
        if (languageSpeaker == null || LanguageWindow == null)
            return;

        LanguageWindow.UpdateState(languageSpeaker.CurrentLanguage, languageSpeaker.SpokenLanguages);
    }

    private void OnLanguageChosen(LanguagePrototype language)
    {
        _languages?.RequestSetLanguage(language);

        // Predict the change
        if (_languages?.GetLocalSpeaker()?.SpokenLanguages is {} languages)
            LanguageWindow?.UpdateState(language.ID, languages);
    }

    /// <summary>
    ///     Moves the language up or down in the player's list of languages. Positive offset means down.
    /// </summary>
    private void MoveLanguage(ProtoId<LanguagePrototype> languageId, int offset)
    {
        if (_languages?.GetLocalSpeaker() is not { } speakerComp)
            return;

        var order = _languages.GetLocalPreferredLanguageOrder();
        if (order == null)
            return;

        var currentIdx = order.IndexOf(languageId);
        if (currentIdx == -1)
            return;

        var newIdx = Math.Clamp(currentIdx + offset, 0, order.Count - 1);

        order.RemoveAt(currentIdx);
        order.Insert(newIdx, languageId);

        _languages.RequestReorderLanguages(order);
        // Predict the change
        LanguageWindow?.UpdateState(speakerComp.CurrentLanguage, order.Where(it => _languages.CanLocalPlayerSpeak(it)).ToList());
    }
}
