using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace ProgressionEducation;

public class LordToil_RingBell(StudyGroup studyGroup) : LordToil
{
    private bool bellRung;

    public override void LordToilTick()
    {
        base.LordToilTick();
        TryRingAutomaticBells();
    }

    private void TryRingAutomaticBells()
    {
        if (bellRung || lord.ticksInToil % 60 != 0)
        {
            return;
        }

        foreach (var bellComp in CompBell.AllBells
                     .Where(bellComp => bellComp.parent.Map == lord.Map
                                        && bellComp.ShouldRingAutomatically))
        {
            bellComp.RingBell();
            bellRung = true;
            EducationLog.Message(
                $"Automatic bell '{
                    bellComp.parent.Label
                }' rang for class '{
                    studyGroup.className
                }'. Sending BellRung memo.");
            lord.ReceiveMemo("BellRung");
            return;
        }
    }

    public override void UpdateAllDuties()
    {
        EducationLog.Message(
            $"LordToil_RingBell.UpdateAllDuties called for class '{studyGroup.className}'");
        if (!GatheringsUtility.PawnCanStartOrContinueGathering(studyGroup.teacher))
        {
            EducationLog.Message($"-> but {studyGroup.teacher.LabelShort} is unavailable. Suspending class.");
            Messages.Message(
                "PE_CannotAttendClass".Translate(studyGroup.className,
                    studyGroup.teacher.LabelShort), MessageTypeDefOf.CautionInput);
            lord.ReceiveMemo(LordJob_AttendClass.MemoClassCancelled);
            studyGroup.Notify_TeacherUnavailable();
            return;
        }
        TryRingAutomaticBells();
        if (!bellRung)
        {
            studyGroup.teacher.mindState.duty =
                new PawnDuty(DefsOf.PE_RingBellDuty, studyGroup.teacher.Position);
            EducationLog.Message(
                $"-> Set teacher {
                    studyGroup.teacher.LabelShort
                } duty to PE_RingBellDuty at position {
                    studyGroup.teacher.Position
                }");
        }
        else
        {
            EducationLog.Message("-> Bell already rung, not setting teacher duty");
        }
    }
}