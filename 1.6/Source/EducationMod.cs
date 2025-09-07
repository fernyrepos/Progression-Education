using HarmonyLib;
using UnityEngine;
using Verse;

namespace ProgressionEducation
{
    public class EducationMod : Mod
    {
        public static EducationSettings settings;
        public EducationMod(ModContentPack content) : base(content)
        {
            settings = GetSettings<EducationSettings>();
            LongEventHandler.ExecuteWhenFinished(delegate
            {
                new Harmony("ProgressionEducationMod").PatchAll();
            });
        }

        public override string SettingsCategory()
        {
            return Content.Name;
        }
        public override void DoSettingsWindowContents(Rect inRect)
        {
            settings.DoSettingsWindowContents(inRect);
        }
    }
}
