using Content.Server._Floof.Speech.Components;
using Content.Shared.Speech;
using Robust.Shared.Random;
using System.Text;

namespace Content.Server._Floof.Speech.EntitySystems;

public sealed class RrrollingAccentSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RrrollingAccentComponent, AccentGetEvent>(OnAccent);
    }


    private void OnAccent(EntityUid uid, RrrollingAccentComponent comp, AccentGetEvent args)
    {
        var message = args.Message;
        StringBuilder replacementMessage = new();
        var messageIndex = 0;
        foreach (var currentLetter in message)
        {
            replacementMessage.Append(currentLetter);
            if (char.ToLower(currentLetter) == 'r')
            {
                var repeats = _random.Next(0, 3);
                char outputLetter;
                if (char.IsUpper(currentLetter) && char.IsUpper(message[messageIndex + 1]))
                    outputLetter = char.ToUpper(currentLetter);
                else
                {
                    outputLetter = char.ToLower(currentLetter);
                }
                for (var i = 0; i < repeats; i++)
                {
                    replacementMessage.Append(outputLetter);
                }
                messageIndex++;
            }
        }
        args.Message = replacementMessage.ToString();
    }
}
