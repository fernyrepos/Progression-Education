using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace ProgressionEducation;

[HotSwappable]
public abstract class JobDriver_LessonBase : JobDriver
{
    public Thing weapon;

    protected StudyGroup StudyGroup
    {
        get
        {
            if (pawn.GetLord()?.LordJob is LordJob_AttendClass lordJob)
            {
                return lordJob.studyGroup;
            }

            return null;
        }
    }

    public void DrawEquipment(Vector3 rootLoc, Rot4 pawnRotation, PawnRenderFlags flags)
    {
        if (weapon == null
            || !StudyGroup.ClassIsActive())
        {
            return;
        }

        var equipmentDrawDistanceFactor = pawn.ageTracker.CurLifeStage.equipmentDrawDistanceFactor;
        PawnRenderUtility.DrawCarriedWeapon(weapon as ThingWithComps, rootLoc,
            pawnRotation,
            equipmentDrawDistanceFactor);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Deep.Look(ref weapon, "weapon");
    }

    public virtual void InitializeWeapon()
    {
    }
}