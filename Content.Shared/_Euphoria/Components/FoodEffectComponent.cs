namespace Content.Shared._Euphoria.Components;

[RegisterComponent]
public sealed partial class FoodEffectComponent : Component
{
    [DataField]
    public StatusEffects Effect = StatusEffects.Speed;

    [DataField]
    public TimeSpan? Time = TimeSpan.FromSeconds(2);

    [DataField]
    public StatusEffectMetabolismMode Mode = StatusEffectMetabolismMode.Update;

    [DataField]
    public TimeSpan Delay;
}

public enum StatusEffects
{
    Speed,
}

public enum StatusEffectMetabolismMode
{
    Update,
    Add,
    Remove,
    Set,
}
