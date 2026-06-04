using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace ProgressionEducation;

[HarmonyPatch(typeof(FloatMenuOptionProvider_Trade), nameof(FloatMenuOptionProvider_Trade.GetOptionsFor))]
public static class FloatMenuOptionProvider_Trade_GetOptionsFor_Patch
{
    public static IEnumerable<FloatMenuOption> Postfix(IEnumerable<FloatMenuOption> __result, Pawn clickedPawn, FloatMenuContext context)
    {
        var isMute = clickedPawn.CanHaveProficiencies()
                     && ProficiencyUtility.IsTrackEnabled(DefsOf.PE_SpeechTrack)
                     && ProficiencyUtility.GetCurrentTier(clickedPawn, DefsOf.PE_SpeechTrack) == DefsOf.PE_MuteTier;

        foreach (var opt in __result)
        {
            if (isMute)
            {
                var socialLabel = SkillDefOf.Social.LabelCap;
                var cannotPrioritizeSocial = "CannotPrioritizeWorkTypeDisabled".Translate(socialLabel);
                if (opt.Label.Contains(cannotPrioritizeSocial))
                {
                    opt.Label = "PE_CannotTradeMute".Translate(clickedPawn.LabelShort);
                }
            }
            yield return opt;
        }
    }
}
