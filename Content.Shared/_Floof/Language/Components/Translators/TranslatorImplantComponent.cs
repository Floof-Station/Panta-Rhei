namespace Content.Shared._Floof.Language.Components.Translators;

/// <summary>
///     An implant that allows the implantee to speak and understand other languages.
/// </summary>
[RegisterComponent]
public sealed partial class TranslatorImplantComponent : BaseTranslatorComponent
{
    /// <summary>
    ///     Whether the implantee knows the languages necessary to speak using this implant.
    /// </summary>
    [ViewVariables]
    public bool SpokenRequirementSatisfied = false;

    /// <summary>
    ///     Whether the implantee knows the languages necessary to understand translations of this implant.
    /// </summary>
    [ViewVariables]
    public bool UnderstoodRequirementSatisfied = false;
}
