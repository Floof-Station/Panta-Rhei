using Content.Shared.Chat;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._Floof.InteractionVerbs;

/// <summary>
///     Represents a verb category defined in yaml. See <see cref="Content.Shared.Verbs.VerbCategory"/>
/// </summary>
[Prototype]
public sealed partial class VerbCategoryPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; set;  } = default!;

    [DataField(required: true)]
    public LocId Name;

    [DataField]
    public SpriteSpecifier.Texture? Icon = null;

    [DataField]
    public int Columns = 1;

    [DataField]
    public bool IconsOnly = false;

    public VerbCategory Materialize() => new(Name, Icon?.TexturePath.ToString(), IconsOnly)
    {
        Columns = Columns,
    };
}
