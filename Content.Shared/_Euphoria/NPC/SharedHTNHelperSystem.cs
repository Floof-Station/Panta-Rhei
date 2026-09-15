namespace Content.Shared._Euphoria.NPC;

public abstract class SharedHtnHelperSystem : EntitySystem
{
    /// <summary>
    ///     Helper method to call NPCSystem.SetBlackboard from shared. Does nothing on the client side.
    /// </summary>
    public virtual void SetBlackboard(EntityUid uid, string key, object value) { }
}
