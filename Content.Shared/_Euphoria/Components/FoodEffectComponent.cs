namespace Content.Shared._Euphoria.FoodEffects.Components;

[RegisterComponent]
public sealed partial class FoodEffectComponent : Component
{
    [DataField]
    public string Effect = "";

    [DataField]
    public TimeSpan? Time = TimeSpan.FromSeconds(2);

    [DataField]
    public StatusEffectMetabolismMode Mode = StatusEffectMetabolismMode.Update;

    [DataField]
    public TimeSpan Delay;
}

public enum StatusEffectMetabolismMode
{
    Update,
    Add,
    Remove,
    Set,
}
