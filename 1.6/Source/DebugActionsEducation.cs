using System.Collections.Generic;
using LudeonTK;
using Verse;

namespace ProgressionEducation;

public static class DebugActionsEducation
{
    [DebugAction("General", "Set proficiency", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
    private static void SetProficiency(Pawn p)
    {
        if (p == null || !p.CanHaveProficiencies()) return;
        var options = new List<DebugMenuOption>();
        foreach (var track in DefDatabase<ProficiencyDef>.AllDefsListForReading)
        {
            foreach (var tier in track.tiers)
            {
                var localTrack = track;
                var localTier = tier;
                options.Add(new DebugMenuOption($"{localTrack.label.CapitalizeFirst()} - {localTier.label.CapitalizeFirst()}", DebugMenuOptionMode.Action, () =>
                {
                    ProficiencyUtility.GrantTier(p, localTrack, localTier);
                    DebugActionsUtility.DustPuffFrom(p);
                }));
            }
        }
        Find.WindowStack.Add(new Dialog_DebugOptionListLister(options));
    }
}
