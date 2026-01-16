using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Floof.Paint;

/// <summary>
/// Applied to an entity that has been painted using a spray paint (NOT a spray painter!)
/// </summary>
/// <remarks>Floofstation: renamed to ColorPainted due to a conflict with SprayPainter</remarks>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ColorPaintedComponent : Component
{
    [DataField, AutoNetworkedField]
    public Color Color = Color.FromHex("#2cdbd5");

    /// Used to remove the color when the component is removed
    [DataField, AutoNetworkedField]
    public Color BeforeColor;

    [DataField, AutoNetworkedField]
    public bool Enabled;

    // Not using ProtoId because ShaderPrototype is in Robust.Client
    [DataField, AutoNetworkedField]
    public string ShaderName = "Greyscale";
}

[Serializable, NetSerializable]
public enum PaintVisuals : byte
{
    Painted,
}
