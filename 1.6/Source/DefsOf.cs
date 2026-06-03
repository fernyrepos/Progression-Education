using RimWorld;
using Verse;
using Verse.AI;

namespace ProgressionEducation
{
    [DefOf]
    public static class DefsOf
    {
        public static JobDef PE_AttendClass;
        [DefAlias("PE_AttendClass")] public static DutyDef PE_AttendClassDuty;
        public static JobDef PE_AttendMeleeClass;
        public static JobDef PE_AttendShootingClass;
        public static StatDef PE_ClassSpeed;
        public static TraitDef PE_FirearmProficiency;
        public static ProficiencyDef PE_WeaponTrack;
        public static ProficiencyDef PE_VehicleTrack;
        public static ProficiencyDef PE_SpeechTrack;

        [MayRequire("MemeGoddess.GiddyUp")]
        public static ProficiencyTierDef PE_AnimalRidingTier;
        [MayRequire("SmashPhil.VehicleFramework")]
        public static ProficiencyTierDef PE_AutomobileDrivingTier;
        [MayRequire("SmashPhil.VehicleFramework")]
        public static ProficiencyTierDef PE_AerialPilotingTier;
        [MayRequire("ludeon.rimworld.odyssey")]
        public static ProficiencyTierDef PE_OrbitalPilotingTier;
        public static ProficiencyTierDef PE_MuteTier;
        public static ProficiencyTierDef PE_FirearmTier;
        public static TraitDef PE_HighTechProficiency;
        public static TraitDef PE_LowTechProficiency;
        public static JobDef PE_RingBell;
        [DefAlias("PE_RingBell")] public static DutyDef PE_RingBellDuty;
        public static JobDef PE_Teach;
        [DefAlias("PE_Teach")] public static DutyDef PE_TeachDuty;
        public static MainButtonDef Schedule;

        static DefsOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(DefsOf));
        }
    }
}
