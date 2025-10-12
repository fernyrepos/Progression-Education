using RimWorld;
using Verse;
using Verse.AI;

namespace ProgressionEducation
{
    [DefOf]
    public static class DefsOf
    {
        [DefAlias("PE_AttendClass")] public static DutyDef PE_AttendClassDuty;
        [DefAlias("PE_Teach")] public static DutyDef PE_TeachDuty;
        [DefAlias("PE_RingBell")] public static DutyDef PE_RingBellDuty;

        public static JobDef PE_AttendClass;
        public static JobDef PE_AttendMeleeClass;
        public static JobDef PE_AttendShootingClass;
        public static JobDef PE_RingBell;
        public static JobDef PE_Teach;

        public static ThingDef PE_SchoolBell;
        public static ThingDef PE_ElectricSchoolBell;
        public static ThingDef PE_ShootingTarget;
        public static ThingDef PE_Gun_AssaultRifleTraining;
        [DefAlias("PE_SchoolBell")] public static SoundDef PE_SchoolBellSound;
        [DefAlias("PE_ElectricSchoolBell")] public static SoundDef PE_ElectricSchoolBellSound;

        public static TraitDef PE_LowTechProficiency;
        public static TraitDef PE_FirearmProficiency;
        public static TraitDef PE_HighTechProficiency;

        public static MainButtonDef Schedule;


        static DefsOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(DefsOf));
        }
    }
}
