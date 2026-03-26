using System.Linq;
using Content.Shared._Floof.Traits.Components;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;

namespace Content.Shared.Mind.Filters;

/// <summary>
/// A mind filter that checks the mind's owned entity for the Marked component, and checks its flags.
/// </summary>
public sealed partial class MarkedMindFilter : MindFilter
{
    [DataField(required: true)]
    public ObjectiveTypes ObjType = new();

    [DataField(required: false)]
    public List<string> OverrideDepts = [];

    protected override bool ShouldRemove(Entity<MindComponent> ent, EntityUid? exclude, IEntityManager entMan, SharedMindSystem mindSys)
    {
        //Skip this block if no special consideration for departments
        if (OverrideDepts.Count > 0)
        {
            //Prepares by grabbing the job of the mind being checked
            var jobSys = entMan.System<SharedJobSystem>();
            jobSys.MindTryGetJobId(ent, out var mindjob);
            //Gets the departments of the job, this allows Command to be discovered
            if (mindjob != null && jobSys.TryGetAllDepartments(mindjob, out var jobdept))
            {
                foreach (var dept in jobdept)
                {
                    //Checks if any department for the job is in the override list,
                    //keeps the mind in the list if there is a match and skips
                    //checking for Marked traits
                    if (OverrideDepts.Contains(dept.ID))
                        return false;
                }
            }
        }

        //Checks for Marked component, excludes otherwise
        if (!entMan.TryGetComponent<MarkedComponent>(ent.Comp.CurrentEntity, out var mcomp))
            return true;

        //Checks if the current objective type is Marked
        if (mcomp.TargetType.HasFlag(ObjType))
            return false;

        //Any mind that gets here does not have the objective type Marked and is removed
        return true;
    }
}
