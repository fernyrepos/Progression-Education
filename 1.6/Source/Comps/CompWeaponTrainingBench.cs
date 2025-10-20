using Verse;

namespace ProgressionEducation
{
    public class CompProperties_WeaponTrainingBench : CompProperties
    {
        public ThingDef weaponDef;

        public CompProperties_WeaponTrainingBench()
        {
            compClass = typeof(CompWeaponTrainingBench);
        }
    }

    public class CompWeaponTrainingBench : ThingComp
    {
        public CompProperties_WeaponTrainingBench Props => (CompProperties_WeaponTrainingBench)props;

        public ThingDef WeaponDef => Props.weaponDef;
    }
}