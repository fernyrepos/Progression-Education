using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace ProgressionEducation
{
    [HarmonyPatch(typeof(ResearchManager), nameof(ResearchManager.FinishProject))]
    public static class ResearchManager_FinishProject_Patch
    {
        public static void Postfix(ResearchProjectDef proj)
        {
            if (proj.HasModExtension<ResearchGrantsTrait>())
            {
                ResearchGrantsTrait extension = proj.GetModExtension<ResearchGrantsTrait>();
                TraitDef traitDef = extension.trait;
                
                if (traitDef != null)
                {
                    List<Pawn> pawns = PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_OfPlayerFaction;
                    foreach (Pawn pawn in pawns)
                    {
                        if (pawn.RaceProps.Humanlike && pawn.Faction == Faction.OfPlayer && !pawn.Dead && pawn.story != null)
                        {
                            if (!pawn.story.traits.HasTrait(traitDef))
                            {
                                pawn.story.traits.GainTrait(new Trait(traitDef));
                            }
                        }
                    }
                }
            }
        }
    }
}