using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace ProgressionEducation
{
    public class ProficiencyTierDef : Def
    {
        public TraitDef traitDef;
        public string iconPath;
        public ThingDef trainingWeaponDef;
        public int semesterGoal = 30000;
        public TechLevel generationTechLevel = TechLevel.Undefined;
        public bool isDefaultAdult;
        public bool isDefaultChild;
        public List<string> legacyNames = new();

        [Unsaved(false)]
        public Texture2D icon;

        public override void PostLoad()
        {
            if (!iconPath.NullOrEmpty())
            {
                LongEventHandler.ExecuteWhenFinished(() => icon = ContentFinder<Texture2D>.Get(iconPath));
            }
        }
    }
}