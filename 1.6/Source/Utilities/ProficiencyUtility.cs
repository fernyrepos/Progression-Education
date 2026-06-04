using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using UnityEngine;
using HarmonyLib;

namespace ProgressionEducation;

[HotSwappable]
[StaticConstructorOnStartup]
public static class ProficiencyUtility
{
    private static readonly Dictionary<ThingDef, TechLevel> cachedTechLevelValues = new();

    private static readonly Texture2D CircleBrightTex = ContentFinder<Texture2D>.Get("UI/CircleBright");
    private static readonly Texture2D CircleDarkTex = ContentFinder<Texture2D>.Get("UI/CircleDark");

    public static bool AreVehicleModsActive => ModsConfig.OdysseyActive || ModsConfig.IsActive("MemeGoddess.GiddyUp") || ModsConfig.IsActive("SmashPhil.VehicleFramework");

    public static bool CanHaveProficiencies(this Pawn pawn)
    {
        if (EducationMod.settings.enableProficiencySystem is false) return false;
        if (pawn?.story?.traits == null)
        {
            return false;
        }
        return pawn.RaceProps.Humanlike;
    }

    public static void ApplyProficiencyTraitToPawn(Pawn pawn)
    {
        if (pawn.CanHaveProficiencies() is false)
        {
            return;
        }

        var techLevel = pawn.Faction?.def?.techLevel ?? TechLevel.Undefined;

        foreach (var track in DefDatabase<ProficiencyDef>.AllDefsListForReading)
        {
            if (!IsTrackEnabled(track)) continue;

            if (GetCurrentTier(pawn, track) == null)
            {
                var bestTier = DetermineTierForPawn(pawn, track, techLevel);

                if (bestTier != null)
                {
                    GrantTier(pawn, track, bestTier);
                }
            }
        }
    }

    private static ProficiencyTierDef DetermineTierForPawn(Pawn pawn, ProficiencyDef track, TechLevel techLevel)
    {
        if (pawn.DevelopmentalStage == DevelopmentalStage.Newborn || pawn.DevelopmentalStage == DevelopmentalStage.Baby)
        {
            return track.tiers.Count > 0 ? track.tiers[0] : null;
        }

        if (track == DefsOf.PE_VehicleTrack)
        {
            var roll = Rand.Value;
            if (techLevel == TechLevel.Archotech)
            {
                return DefsOf.PE_OrbitalPilotingTier ?? DefsOf.PE_AerialPilotingTier ?? DefsOf.PE_AutomobileDrivingTier ?? DefsOf.PE_AnimalRidingTier;
            }
            if (techLevel == TechLevel.Ultra)
            {
                var best = roll < 0.9f ? DefsOf.PE_OrbitalPilotingTier : DefsOf.PE_AerialPilotingTier;
                return best ?? DefsOf.PE_OrbitalPilotingTier ?? DefsOf.PE_AerialPilotingTier ?? DefsOf.PE_AutomobileDrivingTier ?? DefsOf.PE_AnimalRidingTier;
            }
            if (techLevel == TechLevel.Spacer)
            {
                ProficiencyTierDef best;
                if (roll < 0.7f) best = DefsOf.PE_OrbitalPilotingTier;
                else if (roll < 0.9f) best = DefsOf.PE_AerialPilotingTier;
                else best = DefsOf.PE_AutomobileDrivingTier;
                return best ?? DefsOf.PE_OrbitalPilotingTier ?? DefsOf.PE_AerialPilotingTier ?? DefsOf.PE_AutomobileDrivingTier ?? DefsOf.PE_AnimalRidingTier;
            }
            if (techLevel == TechLevel.Industrial)
            {
                var best = roll < 0.9f ? DefsOf.PE_AutomobileDrivingTier : DefsOf.PE_AerialPilotingTier;
                return best ?? DefsOf.PE_AerialPilotingTier ?? DefsOf.PE_AutomobileDrivingTier ?? DefsOf.PE_AnimalRidingTier;
            }
            return DefsOf.PE_AnimalRidingTier;
        }

        if (track == DefsOf.PE_SpeechTrack)
        {
            var roll = Rand.Value;
            if (techLevel >= TechLevel.Industrial) return DefsOf.PE_FluentSpeechTier;
            if (techLevel == TechLevel.Medieval)
            {
                if (roll < 0.1f) return DefsOf.PE_MuteTier;
                if (roll < 0.4f) return DefsOf.PE_BasicSpeechTier;
                return DefsOf.PE_FluentSpeechTier;
            }
            if (techLevel == TechLevel.Neolithic)
            {
                if (roll < 0.2f) return DefsOf.PE_MuteTier;
                if (roll < 0.6f) return DefsOf.PE_BasicSpeechTier;
                return DefsOf.PE_FluentSpeechTier;
            }
            return DefsOf.PE_MuteTier;
        }

        var bestTier = track.tiers.Count > 0 ? track.tiers[0] : null;
        foreach (var tier in track.tiers)
        {
            if (tier.generationTechLevel != TechLevel.Undefined && techLevel >= tier.generationTechLevel)
                bestTier = tier;
            if (pawn.DevelopmentalStage == DevelopmentalStage.Adult && tier.isDefaultAdult)
                bestTier = tier;
            if (pawn.DevelopmentalStage == DevelopmentalStage.Child && tier.isDefaultChild)
                bestTier = tier;
        }
        return bestTier;
    }

    public static bool CanEquipItem(this Pawn pawn, Thing equipment)
    {
        if (pawn.CanHaveProficiencies() is false
            || pawn.Faction != Faction.OfPlayerSilentFail
            || PawnGenerator.IsBeingGenerated(pawn))
        {
            return true;
        }

        var proficiencyRequirement = equipment.def.GetModExtension<ItemProficiencyRequirement>();
        TraitDef requiredProficiency = null;
        if (proficiencyRequirement == null || proficiencyRequirement.requiredProficiency == null)
        {
            var techLevel = GetTechLevelFor(equipment.def);
            foreach (var tier in DefsOf.PE_WeaponTrack.tiers)
            {
                if (tier.generationTechLevel != TechLevel.Undefined && techLevel <= tier.generationTechLevel)
                {
                    requiredProficiency = tier.traitDef;
                    break;
                }
            }
            if (requiredProficiency == null)
            {
                requiredProficiency = DefsOf.PE_WeaponTrack.tiers.Last().traitDef;
            }
        }
        else
        {
            requiredProficiency = proficiencyRequirement.requiredProficiency;
        }

        if (requiredProficiency != null)
        {
            if (equipment is Apparel && DefsOf.PE_WeaponTrack.tiers.Any(t => t.traitDef == requiredProficiency))
            {
                return true;
            }

            ProficiencyTierDef requiredTier = null;
            ProficiencyDef requiredTrack = null;
            foreach (var track in DefDatabase<ProficiencyDef>.AllDefsListForReading)
            {
                if (!IsTrackEnabled(track)) continue;
                requiredTier = track.tiers.FirstOrDefault(t => t.traitDef == requiredProficiency);
                if (requiredTier != null)
                {
                    requiredTrack = track;
                    break;
                }
            }
            if (requiredTrack != null)
            {
                return MeetsOrExceedsTier(pawn, requiredTrack, requiredTier);
            }
        }

        return true;
    }

    public static string GetProficiencyLevelString(ThingDef thingDef)
    {
        var techLevel = GetTechLevelFor(thingDef);
        foreach (var tier in DefsOf.PE_WeaponTrack.tiers)
        {
            if (tier.generationTechLevel != TechLevel.Undefined && techLevel <= tier.generationTechLevel)
            {
                return tier.label;
            }
        }
        return DefsOf.PE_WeaponTrack.tiers.Last().label;
    }

    public static TechLevel GetTechLevelFor(ThingDef thingDef)
    {
        if (!cachedTechLevelValues.TryGetValue(thingDef, out var techLevel))
        {
            cachedTechLevelValues[thingDef] = techLevel = GetTechLevelForInt(thingDef);
        }

        return techLevel;
    }

    private static TechLevel GetTechLevelForInt(ThingDef thingDef)
    {
        var techLevelSources = new List<TechLevel>();
        if (thingDef.GetCompProperties<CompProperties_Techprint>() != null)
        {
            techLevelSources.Add(thingDef.GetCompProperties<CompProperties_Techprint>()
                .project
                .techLevel);
        }

        if (thingDef.recipeMaker != null)
        {
            if (thingDef.recipeMaker.researchPrerequisite != null)
            {
                var techLevel = thingDef.recipeMaker.researchPrerequisite.techLevel;
                if (techLevel != TechLevel.Undefined)
                {
                    techLevelSources.Add(techLevel);
                }
            }

            if (thingDef.recipeMaker.researchPrerequisites?.Any() ?? false)
            {
                var num = thingDef.recipeMaker.researchPrerequisites.MaxBy(x => (int)x.techLevel)
                    .techLevel;
                var techLevel = num;
                if (techLevel != TechLevel.Undefined)
                {
                    techLevelSources.Add(techLevel);
                }
            }
        }

        if (thingDef.researchPrerequisites?.Any() ?? false)
        {
            var num = thingDef.researchPrerequisites.MaxBy(x => (int)x.techLevel).techLevel;
            var techLevel = num;
            if (techLevel != TechLevel.Undefined)
            {
                techLevelSources.Add(techLevel);
            }
        }

        techLevelSources.Add(thingDef.techLevel);
        return techLevelSources.MaxBy(x => (int)x);
    }

    public static void DrawKnowledgePanel(Rect rect, Pawn pawn)
    {
        Rect inner = rect;
        float curY = inner.y;
        Text.Font = GameFont.Small;
        Widgets.Label(new Rect(inner.x, curY, inner.width, 24f), "PE_Knowledge".Translate().AsTipTitle());
        curY += 22f;
        Widgets.DrawBoxSolid(new Rect(inner.x, inner.y + 22, inner.width, inner.height - 22), new ColorInt(46, 48, 52).ToColor);
        curY += 2f;
        foreach (var track in DefDatabase<ProficiencyDef>.AllDefsListForReading)
        {
            if (!IsTrackEnabled(track)) continue;
            var currentTier = GetCurrentTier(pawn, track);
            if (currentTier == null)
            {
                ApplyProficiencyTraitToPawn(pawn);
                currentTier = GetCurrentTier(pawn, track);
            }
            DrawProficiencyRow(new Rect(inner.x, curY, inner.width, 22f), track, currentTier);
            curY += 24f;
        }
    }

    private static void DrawProficiencyRow(Rect rect, ProficiencyDef track, ProficiencyTierDef currentTier)
    {
        if (currentTier == null) currentTier = track.tiers[0];
        int currentIndex = track.tiers.IndexOf(currentTier);

        float dotAreaStartX = rect.x + 130f;
        var bubbleRect = new Rect(rect.x, rect.y, dotAreaStartX - rect.x - 6f, rect.height);
        Widgets.DrawHighlightIfMouseover(bubbleRect);

        var activeIconRect = new Rect(rect.x + 4f, rect.y + 2f, 18f, 18f);
        GUI.DrawTexture(activeIconRect, currentTier.icon);

        var labelRect = new Rect(rect.x + 26f, rect.y, dotAreaStartX - rect.x - 32f, rect.height);
        Widgets.Label(labelRect, currentTier.label.CapitalizeFirst());

        var title = currentTier.traitDef.degreeDatas.Count > 0 ? currentTier.traitDef.degreeDatas[0].label : currentTier.label;
        var desc = currentTier.traitDef.degreeDatas.Count > 0 ? currentTier.traitDef.degreeDatas[0].description : currentTier.traitDef.description;
        TooltipHandler.TipRegion(bubbleRect, new TipSignal($"{title.CapitalizeFirst()}\n\n{desc}"));

        var spacing = 22f;

        float curX = dotAreaStartX;
        for (int i = 0; i < track.tiers.Count; i++)
        {
            var tier = track.tiers[i];
            var dotRect = new Rect(curX, rect.y + 2f, 18f, 18f);
            var bgTex = i == currentIndex ? CircleBrightTex : CircleDarkTex;
            GUI.DrawTexture(dotRect, bgTex);
            GUI.color = new Color(0.15f, 0.15f, 0.15f, 1f);
            GUI.DrawTexture(dotRect.ExpandedBy(-3), tier.icon);
            GUI.color = Color.white;
            var dotData = tier.traitDef.degreeDatas[0];
            TooltipHandler.TipRegion(dotRect, new TipSignal($"{dotData.label.CapitalizeFirst()}\n\n{dotData.description}"));
            curX += spacing;
        }
        GUI.color = Color.white;
    }

    public static void GrantProficiencyTrait(Pawn pawn, TraitDef traitToAdd)
    {
        foreach (var track in DefDatabase<ProficiencyDef>.AllDefsListForReading)
        {
            foreach (var tier in track.tiers)
            {
                if (tier.traitDef == traitToAdd)
                {
                    GrantTier(pawn, track, tier);
                    return;
                }
            }
        }
    }

    public static ProficiencyTierDef GetCurrentTier(Pawn pawn, ProficiencyDef track)
    {
        if (pawn.CanHaveProficiencies() is false)
        {
            return null;
        }
        for (var i = track.tiers.Count; i > 0; i--)
        {
            if (pawn.story.traits.HasTrait(track.tiers[i - 1].traitDef)) return track.tiers[i - 1];
        }
        return null;
    }

    public static bool IsProficiencyTrait(TraitDef def)
    {
        return DefDatabase<ProficiencyDef>.AllDefsListForReading.Any(track => track.tiers.Any(t => t.traitDef == def));
    }

    public static bool IsTrackEnabled(ProficiencyDef track)
    {
        if (track == DefsOf.PE_WeaponTrack)
        {
            return EducationMod.settings.enableWeaponProficiency;
        }
        if (track == DefsOf.PE_VehicleTrack)
        {
            return AreVehicleModsActive && EducationMod.settings.enableVehicleProficiency && track.tiers.Count > 0;
        }
        if (track == DefsOf.PE_SpeechTrack)
        {
            return EducationMod.settings.enableSpeechProficiency && track.tiers.Count > 0;
        }
        return track.tiers.Count > 0;
    }

    public static bool MeetsOrExceedsTier(Pawn pawn, ProficiencyDef track, ProficiencyTierDef tier)
    {
        if (pawn.CanHaveProficiencies() is false)
        {
            return false;
        }
        if (GetCurrentTier(pawn, track) == null)
        {
            ApplyProficiencyTraitToPawn(pawn);
        }
        var targetIdx = track.tiers.IndexOf(tier);
        for (var i = targetIdx; i < track.tiers.Count; i++)
        {
            if (pawn.story.traits.HasTrait(track.tiers[i].traitDef))
                return true;
        }
        return false;
    }

    public static bool IsOneTierBelow(Pawn pawn, ProficiencyDef track, ProficiencyTierDef tier)
    {
        if (pawn.CanHaveProficiencies() is false)
        {
            return false;
        }
        var currentTier = GetCurrentTier(pawn, track);
        if (currentTier == null)
        {
            ApplyProficiencyTraitToPawn(pawn);
            currentTier = GetCurrentTier(pawn, track);
        }
        var targetIdx = track.tiers.IndexOf(tier);
        var currentIdx = track.tiers.IndexOf(currentTier);
        return currentIdx == targetIdx - 1;
    }

    public static void GrantTier(Pawn pawn, ProficiencyDef track, ProficiencyTierDef tier)
    {
        if (pawn.CanHaveProficiencies() is false)
        {
            return;
        }
        pawn.story.traits.allTraits.RemoveAll(t => track.tiers.Any(x => x.traitDef == t.def));
        var trait = new Trait(tier.traitDef);
        pawn.story.traits.GainTrait(trait);
        pawn.story.traits.allTraits.Remove(trait);
        pawn.story.traits.allTraits.Insert(0, trait);
    }

    public static (bool canOperate, string failureReason) CheckVehicleOperation(Pawn pawn, object handler)
    {
        if (!EducationMod.settings.enableProficiencySystem || !EducationMod.settings.enableVehicleProficiency)
            return (true, null);

        var roleField = AccessTools.Field(handler.GetType(), "role");
        var role = roleField.GetValue(handler);
        var handlingTypesField = AccessTools.Field(role.GetType(), "handlingTypes");
        var handlingTypes = (int)handlingTypesField.GetValue(role);
        if ((handlingTypes & 1) != 1)
            return (true, null);

        var vehicleField = AccessTools.Field(handler.GetType(), "vehicle");
        var vehicle = vehicleField.GetValue(handler) as Pawn;
        var vehicleDefType = AccessTools.TypeByName("Vehicles.VehicleDef");
        var typeField = AccessTools.Field(vehicleDefType, "type");
        var typeStr = typeField.GetValue(vehicle.def).ToString();
        var reqTier = typeStr == "Air" ? DefsOf.PE_AerialPilotingTier : DefsOf.PE_AutomobileDrivingTier;

        if (!MeetsOrExceedsTier(pawn, DefsOf.PE_VehicleTrack, reqTier))
            return (false, "PE_VehicleProficiencyRequired".Translate(reqTier.label));

        return (true, null);
    }
}
