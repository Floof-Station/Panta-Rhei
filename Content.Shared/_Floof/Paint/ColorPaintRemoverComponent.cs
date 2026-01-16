using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._Floof.Paint;

///  Removes paint from an entity that was painted with spray paint
[RegisterComponent, NetworkedComponent]
[Access(typeof(ColorPaintRemoverSystem))]
public sealed partial class ColorPaintRemoverComponent : Component
{
    /// Sound played when target is cleaned
    [DataField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/Effects/Fluids/watersplash.ogg");

    [DataField]
    public float CleanDelay = 2f;
}
