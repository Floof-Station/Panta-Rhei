using Robust.Shared.Prototypes;

namespace Content.Shared._EE.StationGoal
{
    [Serializable, Prototype("stationGoal")]
    public sealed partial class StationGoalPrototype : IPrototype
    {
        [IdDataFieldAttribute] public string ID { get; } = default!;

        public string Text => Loc.GetString($"station-goal-{ID.ToLower()}");
    }
}
