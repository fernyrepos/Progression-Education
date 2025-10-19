using UnityEngine;
using Verse;

namespace ProgressionEducation
{
    public class EducationSettings : ModSettings
    {
        public float globalLearningSpeedModifier = 1f;
        public float skillClassesLearningSpeedModifier = 1f;
        public float proficiencyClassesLearningSpeedModifier = 1f;
        public float daycareClassesLearningSpeedModifier = 1f;
        public bool enableProficiencySystem = true;
        public bool debugMode = false;
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref globalLearningSpeedModifier, "globalLearningSpeedModifier", 1f);
            Scribe_Values.Look(ref skillClassesLearningSpeedModifier, "skillClassesLearningSpeedModifier", 1f);
            Scribe_Values.Look(ref proficiencyClassesLearningSpeedModifier, "proficiencyClassesLearningSpeedModifier", 1f);
            Scribe_Values.Look(ref daycareClassesLearningSpeedModifier, "daycareClassesLearningSpeedModifier", 1f);
            Scribe_Values.Look(ref enableProficiencySystem, "enableProficiencySystem", true);
            Scribe_Values.Look(ref debugMode, "debugMode", false);
        }

        public void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listing = new();
            listing.Begin(inRect);
            listing.Label("PE_GlobalLearningSpeed".Translate() + ": " + (globalLearningSpeedModifier * 100).ToString("F0") + "%");
            globalLearningSpeedModifier = listing.Slider(globalLearningSpeedModifier, 0.1f, 3.0f);
            listing.Label("PE_SkillClassesLearningSpeed".Translate() + ": " + (skillClassesLearningSpeedModifier * 100).ToString("F0") + "%");
            skillClassesLearningSpeedModifier = listing.Slider(skillClassesLearningSpeedModifier, 0.1f, 3.0f);
            listing.Label("PE_ProficiencyClassesLearningSpeed".Translate() + ": " + (proficiencyClassesLearningSpeedModifier * 100).ToString("F0") + "%");
            proficiencyClassesLearningSpeedModifier = listing.Slider(proficiencyClassesLearningSpeedModifier, 0.1f, 3.0f);
            listing.Label("PE_DaycareClassesLearningSpeed".Translate() + ": " + (daycareClassesLearningSpeedModifier * 100).ToString("F0") + "%");
            daycareClassesLearningSpeedModifier = listing.Slider(daycareClassesLearningSpeedModifier, 0.1f, 3.0f);
            listing.CheckboxLabeled("PE_EnableProficiencySystem".Translate(), ref enableProficiencySystem);

            listing.Gap(12f);
            listing.CheckboxLabeled("PE_EnableDebugMode".Translate(), ref debugMode);
            listing.End();
        }

        public static EducationSettings Instance => EducationMod.settings;
    }
}
