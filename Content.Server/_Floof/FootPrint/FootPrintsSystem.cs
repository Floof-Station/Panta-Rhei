using System.Linq;
using Content.Server.Atmos.Components;
using Content.Shared._EE.Flight;
using Content.Shared._EE.FootPrint;
using Content.Shared._Floof.Footprint;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;
using Content.Shared.Mobs.Components;
using Content.Shared.Standing;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
// DeltaV

namespace Content.Server._Floof.FootPrint;

public sealed class FootPrintsSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly IMapManager _map = default!;
    [Dependency] private readonly IPrototypeManager _protoMan = default!; // Floofstation

    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedFlightSystem _flight = default!; // DeltaV
    [Dependency] private readonly EntityLookupSystem _lookup = default!; // Floofstation
    [Dependency] private readonly StandingStateSystem _standingState = default!; // Floofstation

    private EntityQuery<TransformComponent> _transformQuery;
    private EntityQuery<MobThresholdsComponent> _mobThresholdQuery;
    private EntityQuery<AppearanceComponent> _appearanceQuery;

//    private EntityQuery<LayingDownComponent> _layingQuery;

    public override void Initialize()
    {
        base.Initialize();

        _transformQuery = GetEntityQuery<TransformComponent>();
        _mobThresholdQuery = GetEntityQuery<MobThresholdsComponent>();
        _appearanceQuery = GetEntityQuery<AppearanceComponent>();
//      _layingQuery = GetEntityQuery<LayingDownComponent>();

        SubscribeLocalEvent<FootPrintsComponent, ComponentStartup>(OnStartupComponent);
        SubscribeLocalEvent<FootPrintsComponent, MoveEvent>(OnMove);
    }

    private void OnStartupComponent(EntityUid uid, FootPrintsComponent component, ComponentStartup args)
    {
        component.StepSize = Math.Max(0f, component.StepSize + _random.NextFloat(-0.05f, 0.05f));
        // Floofstation - multiply by humanoid height
        if (TryComp<HumanoidAppearanceComponent>(uid, out var hum))
            component.StepSize *= hum.Height;
    }

    private void OnMove(EntityUid uid, FootPrintsComponent component, ref MoveEvent args)
    {
        #if DEBUG // Floof - do not create footprints on debug, nor in CI tests
                return;
        #endif // Floof section end
        if (_flight.IsFlying(uid)) // DeltaV - Flying players won't make footprints
            return;

        // Floofstation section - extra checks
        if (component.ContainedSolution.Volume <= 0
            || TryComp<PhysicsComponent>(uid, out var physics) && physics.BodyStatus != BodyStatus.OnGround) // Floof: do not create footprints if the entity is flying
            return;

        // are we on a puddle? we exit, ideally we would exchange liquid and DNA with the puddle but meh, too lazy to do that now.
        var entities = _lookup.GetEntitiesIntersecting(uid, LookupFlags.All);
        if (entities.Any(HasComp<PuddleFootPrintsComponent>))
            return;
        // Floofstation section end

        if (false//component.PrintsColor.A <= 0f // Floofstation - no such thing
            || !_transformQuery.TryComp(uid, out var transform)
            || !_mobThresholdQuery.TryComp(uid, out var mobThreshHolds)
            || !_map.TryFindGridAt(_transform.GetMapCoordinates((uid, transform)), out var gridUid, out _))
            return;

        var dragging = _standingState.IsDown(uid); // Floofstation - replaced: mobThreshHolds.CurrentThresholdState is MobState.Critical or MobState.Dead;
        var distance = (transform.LocalPosition - component.StepPos).Length();
        var stepSize = dragging ? component.DragSize : component.StepSize;

        if (!(distance > stepSize))
            return;

        component.RightStep = !component.RightStep;

        var entity = Spawn(component.StepProtoId, CalcCoords(gridUid, component, transform, dragging));
        var footPrintComponent = EnsureComp<FootPrintComponent>(entity);

        footPrintComponent.PrintOwner = uid;
        Dirty(entity, footPrintComponent);

        if (_appearanceQuery.TryComp(entity, out var appearance))
        {
            // Floofstation section - replaced
            var color = component.ContainedSolution.GetColor(_protoMan);
            color.A = Math.Max(0.3f, component.ContainedSolution.FillFraction);

            _appearance.SetData(entity, FootPrintVisualState.State, PickState(uid, dragging), appearance);
            _appearance.SetData(entity, FootPrintVisualState.Color, color, appearance);
            // Floofstation section end
        }

        if (!_transformQuery.TryComp(entity, out var stepTransform))
            return;

        stepTransform.LocalRotation = dragging
            ? (transform.LocalPosition - component.StepPos).ToAngle() + Angle.FromDegrees(-90f)
            : transform.LocalRotation + Angle.FromDegrees(180f);

        // Floofstation - replaced the below
        component.StepPos = transform.LocalPosition;
        if (!TryComp<SolutionContainerManagerComponent>(entity, out var solutionContainer)
            || !_solution.ResolveSolution((entity, solutionContainer), footPrintComponent.SolutionName, ref footPrintComponent.Solution, out var solution))
            return;

        // Transfer from the component to the footprint
        var removedReagents = component.ContainedSolution.SplitSolution(component.FootprintVolume);
        _solution.ForceAddSolution(footPrintComponent.Solution.Value, removedReagents);
        // Floofstation section end
    }

    private EntityCoordinates CalcCoords(EntityUid uid, FootPrintsComponent component, TransformComponent transform, bool state)
    {
        if (state)
            return new EntityCoordinates(uid, transform.LocalPosition);

        var offset = component.RightStep
            ? new Angle(Angle.FromDegrees(180f) + transform.LocalRotation).RotateVec(component.OffsetPrint)
            : new Angle(transform.LocalRotation).RotateVec(component.OffsetPrint);

        return new EntityCoordinates(uid, transform.LocalPosition + offset);
    }

    private FootPrintVisuals PickState(EntityUid uid, bool dragging)
    {
        var state = FootPrintVisuals.BareFootPrint;

        if (_inventory.TryGetSlotEntity(uid, "shoes", out _))
            state = FootPrintVisuals.ShoesPrint;

        if (_inventory.TryGetSlotEntity(uid, "outerClothing", out var suit) && TryComp<PressureProtectionComponent>(suit, out _))
            state = FootPrintVisuals.SuitPrint;

        if (dragging)
            state = FootPrintVisuals.Dragging;

        return state;
    }
}
