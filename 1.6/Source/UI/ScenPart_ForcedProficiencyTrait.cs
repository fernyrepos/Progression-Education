using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace ProgressionEducation
{
    public class ScenPart_ForcedProficiencyTrait : ScenPart_PawnModifier
    {
        private TraitDef trait;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look(ref trait, "trait");
        }

        public override void Randomize()
        {
            chance = 1;
            context = PawnGenerationContext.PlayerStarter;
            hideOffMap = false;
        }

        public override void DoEditInterface(Listing_ScenEdit listing)
        {
            Rect scenPartRect = listing.GetScenPartRect(this, RowHeight);
            if (Widgets.ButtonText(scenPartRect, trait?.LabelCap ?? "PE_SelectTrait".Translate()))
            {
                List<FloatMenuOption> list = new List<FloatMenuOption>();
                var proficiencyTraits = new List<TraitDef>
                {
                    DefsOf.PE_LowTechProficiency,
                    DefsOf.PE_FirearmProficiency,
                    DefsOf.PE_HighTechProficiency
                };

                foreach (TraitDef proficiencyTrait in proficiencyTraits)
                {
                    TraitDef localTrait = proficiencyTrait;
                    list.Add(new FloatMenuOption(localTrait.LabelCap, () => trait = localTrait));
                }
                Find.WindowStack.Add(new FloatMenu(list));
            }
        }

        public override string Summary(Scenario scen)
        {
            if (trait == null)
            {
                return "PE_ScenPart_ForcedProficiencyTrait_NoTrait".Translate();
            }
            return "PE_ScenPart_ForcedProficiencyTrait".Translate(trait.LabelCap).CapitalizeFirst();
        }
        public override void Notify_PawnGenerated(Pawn pawn, PawnGenerationContext context, bool redressed)
        {
            if (context != PawnGenerationContext.PlayerStarter)
            {
                return;
            }
            if (trait != null)
            {
                ProficiencyUtility.GrantProficiencyTrait(pawn, trait);
            }
        }
    }
}
