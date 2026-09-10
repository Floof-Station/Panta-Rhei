using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;
using Robust.Shared.Utility;

namespace Content.Shared._Floof.Ropes.Prototypes;

/// <summary>
///     Describes how a rope should be created.
/// </summary>
[Prototype]
public sealed partial class RopeConfigurationPrototype : IPrototype, IInheritingPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<RopeConfigurationPrototype>))]
    public string[]? Parents { get; private set; }

    [NeverPushInheritance, AbstractDataField]
    public bool Abstract { get; private set; }

    /// <summary>
    ///     Entity prototype that the data entity is made from.
    ///     There is always exactly ONE data entity created per rope. It does not physically interact with anything.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId DataPrototype;

    /// <summary>
    ///     Entity prototype that links of this entity are made from.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId LinkPrototype;

    /// <summary>
    ///     Number of segments this rope is made from. Should never exceed ~10 to avoid performance issues.
    /// </summary>
    [DataField(required: true)]
    public int Segments;

    /// <summary>
    ///     Stiffness in N/m.
    ///     A stiffness of 10N/m means that if a joint making up the rope is 0.1m longer than its normal length, it will exert 1N of force trying to normalize itself.
    ///     Real ropes often have stiffness ranging from 10k to 100k N/m. We keep it on the lower end to avoid issues.
    /// </summary>
    [DataField]
    public float Stiffness = 1e4f;

    /// <summary>
    ///     Sprite drawn in place of the joints making up this entity (if any).
    /// </summary>
    [DataField(required: true)]
    public SpriteSpecifier Sprite = null!;
}
