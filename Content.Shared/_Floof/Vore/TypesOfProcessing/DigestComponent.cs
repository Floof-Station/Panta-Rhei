using System;
using Robust.Shared.GameObjects;
namespace Content.Shared._Floof.Vore;

[RegisterComponent]
public sealed partial class DigestComponent : Component
{
    public Dictionary<EntityUid, float> Health = new();
    public Dictionary<EntityUid, float> Timer = new();
    public HashSet<EntityUid> ActiveDigesting = new();
    // the max health of the prey used for digestion and slow regeneration
    public float Max = 100f;
    // the stage of digestion, used for the popup
    public Dictionary<EntityUid, int> DigestPopupStage = new();
}