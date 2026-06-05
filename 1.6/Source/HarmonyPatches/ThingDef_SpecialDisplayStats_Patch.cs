using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace ProgressionEducation;

[HarmonyPatch(typeof(ThingDef), nameof(ThingDef.SpecialDisplayStats))]
public static class ThingDef_SpecialDisplayStats_Patch
{
    public static IEnumerable<StatDrawEntry> Postfix(IEnumerable<StatDrawEntry> __result, ThingDef __instance, StatRequest req)
    {
        foreach (var entry in __result)
        {
            yield return entry;
        }

        if (!EducationMod.settings.enableProficiencySystem) yield break;

        if (__instance.IsWeapon && EducationMod.settings.enableWeaponProficiency)
        {
            var reqExt = __instance.GetModExtension<ItemProficiencyRequirement>();
            var requiredTrait = reqExt?.requiredProficiency;
            var label = "";
            if (requiredTrait == null)
            {
                var techLevel = ProficiencyUtility.GetTechLevelFor(__instance);
                foreach (var tier in DefsOf.PE_WeaponTrack.tiers.Where(tier => tier.generationTechLevel != TechLevel.Undefined && techLevel <= tier.generationTechLevel))
                {
                    label = tier.label;
                    break;
                }
                if (label.NullOrEmpty()) label = DefsOf.PE_WeaponTrack.tiers.Last().label;
            }
            else
            {
                label = requiredTrait.degreeDatas[0].label;
            }
            yield return new StatDrawEntry(StatCategoryDefOf.Weapon, "PE_RequiredProficiencyStat".Translate(), label.CapitalizeFirst(), "PE_RequiredProficiencyStatDesc".Translate(), 5000);
        }

        if (__instance.HasComp(typeof(CompShuttle)) && EducationMod.settings.enableVehicleProficiency)
        {
            yield return new StatDrawEntry(StatCategoryDefOf.Basics, "PE_RequiredProficiencyStat".Translate(), DefsOf.PE_OrbitalPilotingTier.label.CapitalizeFirst(), "PE_RequiredProficiencyStatVehicleDesc".Translate(), 5000);
        }
    }
}
