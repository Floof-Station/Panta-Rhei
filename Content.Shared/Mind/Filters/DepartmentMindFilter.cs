using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Robust.Shared.Prototypes;

namespace Content.Shared.Mind.Filters;

/// <summary>
/// A mind filter that requires minds to have a specific department.
/// </summary>
public sealed partial class DepartmentMindFilter : MindFilter
{
    [DataField(required: true)]
    public ProtoId<DepartmentPrototype> Dept;

    protected override bool ShouldRemove(Entity<MindComponent> mind, EntityUid? exclude, IEntityManager entMan, SharedMindSystem mindSys)
    {
        var jobSys = entMan.System<SharedJobSystem>();
        jobSys.MindTryGetJobName(mind, out var mindjob);
        mindjob = mindjob.Replace(" ", string.Empty);
        if (jobSys.TryGetPrimaryDepartment(mindjob, out var jobdept))
        {
            //Returns false if Dept matches, which does not remove the mind.
            return jobdept != Dept;
        }
        //Any mind that gets here is removed.
        return true;
    }
}
