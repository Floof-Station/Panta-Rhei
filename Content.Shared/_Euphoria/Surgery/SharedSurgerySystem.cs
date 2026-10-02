using Content.Shared._Euphoria.Surgery.Components;
using Content.Shared.GameTicking;
using Content.Shared.Body;
using Content.Shared.Kitchen.Components;
using Content.Shared.Verbs;
using Robust.Shared.Utility;

namespace Content.Shared._Euphoria.Surgery;
//This is where to cause the updates to happen from server to client
public abstract partial class SharedSurgerySystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        //UpdateStates(frameTime)
        //UpdateActions(frameTime)
        //UpdateUI(frameTime);
    }
}
