using Content.Shared._Floof.Examine;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Server._Floof.Examine;

/// <summary>
///     Do-after event for changing examine data on a different entity.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class SetCustomExamineDoAfterEvent : DoAfterEvent
{
    [DataField]
    public CustomExamineData PublicData, SubtleData;

    public SetCustomExamineDoAfterEvent(CustomExamineData publicData, CustomExamineData subtleData)
    {
        PublicData = publicData;
        SubtleData = subtleData;
    }

    // Why are we doing this again? All upstream Clone() implementations just return `this`, and it doesn't seem to need cloning in the first place
    public override DoAfterEvent Clone() => new SetCustomExamineDoAfterEvent(PublicData, SubtleData);
}
