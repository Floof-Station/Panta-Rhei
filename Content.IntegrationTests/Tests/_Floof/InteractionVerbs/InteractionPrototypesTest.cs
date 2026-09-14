using Content.IntegrationTests.Fixtures;
using Content.Shared._Floof.InteractionVerbs;

namespace Content.IntegrationTests.Tests._Floof.InteractionVerbs;

[TestFixture]
[TestOf(typeof(InteractionVerbPrototype))]
public sealed class InteractionPrototypesTest : GameTest
{
    [Test]
    public async Task ValidatePrototypeContents()
    {
        // TODO probably should test if an entity receives an abstract verb, but Iunno how
        foreach (var proto in SProtoMan.EnumeratePrototypes<InteractionVerbPrototype>())
        {
            Assert.That(proto.Abstract || proto.Action is not null, $"Non-abstract prototype {proto.ID} lacks an action!");
        }
    }
}
