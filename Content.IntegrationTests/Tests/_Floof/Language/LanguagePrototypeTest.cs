using System.Collections.Generic;
using System.Linq;
using Content.Shared._Floof.Language;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Floof.Language;

/// <summary>
///    Checks if every language has a valid name, chatname, and description localization string.
/// </summary>
[TestFixture]
[TestOf(typeof(LanguagePrototype))]
public sealed class LanguagePrototypeTest
{
    [Test]
    public async Task CheckLanguagePrototypes()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var locale = server.ResolveDependency<ILocalizationManager>();
        var proto = server.ResolveDependency<IPrototypeManager>();

        await server.WaitAssertion(() =>
        {
            var missingStrings = new List<string>();

            foreach (var langProto in proto.EnumeratePrototypes<LanguagePrototype>().OrderBy(a => a.ID))
                foreach (var locString in new List<string> { $"language-{langProto.ID}-name", $"language-{langProto.ID}-description" })
                    if (!locale.HasString(locString))
                        missingStrings.Add($"\"{langProto.ID}\", \"{locString}\"");

            Assert.That(!missingStrings.Any(), Is.True, $"The following languages are missing localization strings:\n  {string.Join("\n  ", missingStrings)}");
        });

        // Floofstation - also validate additional strings
        await server.WaitAssertion(() =>
        {
            var missingLocaleIds = new List<(LanguagePrototype, LocId)>();
            foreach (var langProto in proto.EnumeratePrototypes<LanguagePrototype>().OrderBy(a => a.ID))
            {
                foreach (var locString in langProto.SpeechOverride.MessageWrapOverrides.Values)
                    if (!locale.HasString(locString))
                        missingLocaleIds.Add((langProto, locString));

                foreach (var locString in langProto.SpeechOverride.SpeechVerbOverrides ?? new())
                    if (!locale.HasString(locString))
                        missingLocaleIds.Add((langProto, locString));
            }

            Assert.That(
                missingLocaleIds,
                Is.Empty,
                $"The following locale strings are missing:\n" +
                $"{string.Join("\n", missingLocaleIds.Select(a => $"\"{a.Item1.ID}\": \"{a.Item2}\""))}");

            // Finally ensure all languages are valid
            foreach (var langProto in proto.EnumeratePrototypes<LanguagePrototype>().OrderBy(a => a.ID))
            {
                var speech = langProto.SpeechOverride;
                var id = langProto.ID;
                Assert.That(!(speech.AllowWriting && speech.RequireHands), $"Language {id} requires hands to speak but can be written? Sus.");
                Assert.That(!(speech.AllowRadio && speech.RequireHands) || speech.RequireSpeech, $"Language {id} is non-spoken but can be transmitted over radio? Sus.");
                Assert.That(!(speech.AllowRadio && speech.RequireLOS), $"Language {id} requires LOS but can be transmitted over radio? Sus.");
            }
        });

        await pair.CleanReturnAsync();
    }
}
