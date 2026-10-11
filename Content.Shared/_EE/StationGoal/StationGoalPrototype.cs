using Robust.Shared.Prototypes;

namespace Content.Shared._EE.StationGoal
{
    [Prototype]
    public sealed partial class StationGoalPrototype : IPrototype
    {
        [IdDataField] public string ID { get; set; } = default!;

        public string Text => Loc.GetString($"station-goal-{ID.ToLower()}");
    }
}
