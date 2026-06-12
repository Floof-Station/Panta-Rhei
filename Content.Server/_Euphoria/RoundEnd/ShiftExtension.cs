using Content.Server.Fax;
using Content.Server.Shuttles.Components;
using Content.Server.Station.Systems;
using Content.Shared.Fax.Components;
using Content.Shared.Paper;
using Content.Shared.Station.Components;

namespace Content.Server.RoundEnd;

public sealed partial class RoundEndSystem : EntitySystem
{
    [Dependency] private readonly FaxSystem _fax = default!;
    /// <summary>
    ///     Send a shift extension review to all faxes that are authorized to receive it.
    /// </summary>
    /// <returns>True if at least one fax received paper.</returns>
    public bool SendShiftExtensionReview()
    {
        //var station = GetStation();
        // if (!HasComp<StationEmergencyShuttleComponent>(station))
        // {
        //     return false;
        // }
        var enumerator = EntityQueryEnumerator<FaxMachineComponent>();
        var wasSent = false;

        while (enumerator.MoveNext(out var uid, out var fax))
        {
            if (!fax.ReceiveShiftExtensionReview)
                continue;

            var printout = new FaxPrintout(
                Loc.GetString("round-end-system-vote-stalemate-fax-contents"),
                Loc.GetString("round-end-system-vote-stalemate-fax-name"),
                null,
                "ShiftExtensionReviewPaper",
                "paper_stamp-centcom",
                new List<StampDisplayInfo>
                {
                    new StampDisplayInfo
                    {
                        StampedName = Loc.GetString("stamp-component-stamped-name-centcom"),
                        StampedColor = Color.FromHex("#006600")
                    },
                },
                true
            );

            _fax.Receive(uid, printout, null, fax);

            wasSent = true;
        }

        // if (wasSent)
        // {
        //     var msg = Loc.GetString("round-end-system-vote-stalemate-fax-announcement");
        //     _chatSystem.DispatchStationAnnouncement(station.Value, msg, colorOverride: Color.Green);
        // }

        return wasSent;

    }
}