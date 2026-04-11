using RimWorld;
using Verse;
using Verse.AI;

namespace ProgressionEducation;

[DefOf]
public static class DefsOf
{
    public static JobDef PE_AttendClass;
    [DefAlias("PE_AttendClass")] public static DutyDef PE_AttendClassDuty;
    public static JobDef PE_AttendMeleeClass;
    public static JobDef PE_AttendShootingClass;
    public static StatDef PE_ClassSpeed;
    public static TraitDef PE_FirearmProficiency;
    public static ThingDef PE_Gun_AssaultRifleTraining;

    public static ThingDef PE_Gun_SpacerTraining;
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