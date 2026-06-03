using UnityEngine;
using Verse;

namespace ProgressionEducation;

public class EducationSettings : ModSettings
{
    public float daycareClassesLearningSpeedModifier = 1f;
    public bool debugMode;
    public bool enableProficiencySystem = true;
    public float globalLearningSpeedModifier = 1f;
    public float proficiencyClassesLearningSpeedModifier = 1f;
    public float skillClassesLearningSpeedModifier = 1f;
    public bool enableKnowledgePanel = true;
    public bool bestowProficiencyToAll = false;
    public bool enableWeaponProficiency = true;
    public bool enableVehicleProficiency = true;
    public bool enableSpeechProficiency = true;

    public void DoSettingsWindowContents(Rect inRect)
    {
        Listing_Standard listing = new();
        listing.Begin(inRect);
        listing.Label("PE_GlobalLearningSpeed".Translate()
                      + ": "
                      + (globalLearningSpeedModifier * 100).ToString("F0")
                      + "%");
        globalLearningSpeedModifier =
            listing.Slider(globalLearningSpeedModifier, 0.1f, 3.0f);
        listing.Label("PE_SkillClassesLearningSpeed".Translate()
                      + ": "
                      + (skillClassesLearningSpeedModifier * 100).ToString("F0")
                      + "%");
        skillClassesLearningSpeedModifier =
            listing.Slider(skillClassesLearningSpeedModifier, 0.1f, 3.0f);
        listing.Label("PE_ProficiencyClassesLearningSpeed".Translate()
                      + ": "
                      + (proficiencyClassesLearningSpeedModifier * 100).ToString("F0")
                      + "%");
        proficiencyClassesLearningSpeedModifier =
            listing.Slider(proficiencyClassesLearningSpeedModifier, 0.1f, 3.0f);
        listing.Label("PE_DaycareClassesLearningSpeed".Translate()
                      + ": "
                      + (daycareClassesLearningSpeedModifier * 100).ToString("F0")
                      + "%");
        daycareClassesLearningSpeedModifier =
            listing.Slider(daycareClassesLearningSpeedModifier, 0.1f, 3.0f);
        listing.CheckboxLabeled("PE_EnableProficiencySystem".Translate(),
            ref enableProficiencySystem);

        listing.Gap();
        listing.CheckboxLabeled("PE_EnableKnowledgePanel".Translate(), ref enableKnowledgePanel);
        listing.CheckboxLabeled("PE_BestowProficiencyToAll".Translate(), ref bestowProficiencyToAll);
        listing.CheckboxLabeled("PE_EnableWeaponProficiency".Translate(), ref enableWeaponProficiency);
        listing.CheckboxLabeled("PE_EnableVehicleProficiency".Translate(), ref enableVehicleProficiency);
        listing.CheckboxLabeled("PE_EnableSpeechProficiency".Translate(), ref enableSpeechProficiency);
        listing.Gap();
        listing.CheckboxLabeled("PE_EnableDebugMode".Translate(), ref debugMode);
        listing.End();
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref globalLearningSpeedModifier,
            "globalLearningSpeedModifier", 1f);
        Scribe_Values.Look(ref skillClassesLearningSpeedModifier,
            "skillClassesLearningSpeedModifier", 1f);
        Scribe_Values.Look(ref proficiencyClassesLearningSpeedModifier,
            "proficiencyClassesLearningSpeedModifier", 1f);
        Scribe_Values.Look(ref daycareClassesLearningSpeedModifier,
            "daycareClassesLearningSpeedModifier", 1f);
        Scribe_Values.Look(ref enableProficiencySystem, "enableProficiencySystem",
            true);
        Scribe_Values.Look(ref debugMode, "debugMode");
        Scribe_Values.Look(ref enableKnowledgePanel, "enableKnowledgePanel", true);
        Scribe_Values.Look(ref bestowProficiencyToAll, "bestowProficiencyToAll", false);
        Scribe_Values.Look(ref enableWeaponProficiency, "enableWeaponProficiency", true);
        Scribe_Values.Look(ref enableVehicleProficiency, "enableVehicleProficiency", true);
        Scribe_Values.Look(ref enableSpeechProficiency, "enableSpeechProficiency", true);
    }
}
