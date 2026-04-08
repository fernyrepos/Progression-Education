using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace ProgressionEducation;

[HotSwappable]
public class JobDriver_Teach : JobDriver_LessonBase
{
    public SkillDef taughtSkill;
    public int waitingTicks;

    private void DoTeachingInterval(int delta)
    {
        pawn.GainComfortFromCellIfPossible(delta, true);
        if (!StudyGroup.ClassIsActive())
        {
            return;
        }

        pawn.skills?.Learn(SkillDefOf.Social, 0.1f * delta);

        if (!StudyGroup.subjectLogic.IsInfinite)
        {
            StudyGroup.AddProgress(StudyGroup.subjectLogic.ProgressPerTick * delta);
        }

        foreach (var student in StudyGroup.students
                     .Where(StudyGroup.IsStudentPresentAndAttending))
        {
            StudyGroup.subjectLogic.ApplyTeachingTick(student, this,
                delta);
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Defs.Look(ref taughtSkill, "taughtSkill");
        Scribe_Values.Look(ref waitingTicks, "waitingTicks");
    }

    public override string GetReport()
    {
        if (!(StudyGroup?.ClassIsActive() ?? true))
        {
            return "PE_JobReport_WaitingForStudents".Translate();
        }

        return base.GetReport();
    }

    public override void InitializeWeapon()
    {
        if (StudyGroup?.subjectLogic is not ProficiencyClassLogic proficiencyLogic)
        {
            return;
        }

        if (proficiencyLogic.ProficiencyFocus switch
            {
                ProficiencyLevel.Firearm => DefsOf.PE_Gun_AssaultRifleTraining,
                ProficiencyLevel.HighTech => DefsOf.PE_Gun_SpacerTraining,
                _ => null,
            } is ThingDef weaponDef)
        {
            weapon = ThingMaker.MakeThing(weaponDef,
                GenStuff.DefaultStuffFor(weaponDef));
        }
    }

    public override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDespawnedOrNull(TargetIndex.A);
        this.FailOn(() => pawn.mindState.duty.def != DefsOf.PE_TeachDuty);
        this.FailOn(() => StudyGroup == null || StudyGroup.teacher != pawn);
        var learningBoard = job.GetTarget(TargetIndex.A).Thing;
        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
        var wanderToil = new Toil
        {
            initAction = () =>
            {
                var waypoints = learningBoard.GetWaypointsInFrontOfBoard(pawn);
                if (!waypoints.Any())
                {
                    return;
                }

                job.SetTarget(TargetIndex.B, waypoints.RandomElement());
                pawn.pather.StartPath(job.GetTarget(TargetIndex.B),
                    PathEndMode.OnCell);
            },
            defaultCompleteMode = ToilCompleteMode.PatherArrival,
            tickIntervalAction = delta => waitingTicks += delta,
        };
        wanderToil.FailOn(() => StudyGroup == null);
        yield return wanderToil;

        var waitToil = Toils_General.Wait(250);
        waitToil.handlingFacing = true;
        waitToil.socialMode = RandomSocialMode.Off;
        waitToil.tickIntervalAction = delta =>
        {
            pawn.GainComfortFromCellIfPossible(delta, true);
            pawn.rotationTracker.FaceCell(pawn.Position + learningBoard.Rotation.FacingCell);
            waitingTicks += delta;
        };
        waitToil.FailOn(() => StudyGroup == null);
        yield return waitToil;

        yield return Toils_Jump.JumpIf(wanderToil,
            () => StudyGroup != null && !StudyGroup.ClassIsActive());

        var gotoWaypoint = Toils_Goto.GotoCell(TargetIndex.B, PathEndMode.OnCell);
        yield return gotoWaypoint;
        var teachAtWaypoint = new Toil
        {
            defaultCompleteMode = ToilCompleteMode.Delay,
            defaultDuration = 300,
            handlingFacing = true,
            initAction = delegate
            {
                if (StudyGroup.subjectLogic is ProficiencyClassLogic)
                {
                    pawn.rotationTracker.FaceCell(pawn.Position
                                                  + learningBoard.Rotation.FacingCell);
                }
                else
                {
                    pawn.rotationTracker.FaceCell(learningBoard.Position);
                }
            },
            tickIntervalAction = DoTeachingInterval,
        };
        teachAtWaypoint.AddPreInitAction(InitializeWeapon);
        yield return teachAtWaypoint;
        yield return Toils_Jump.JumpIf(wanderToil,
            () => StudyGroup != null && !StudyGroup.ClassIsActive());

        yield return Toils_Jump.JumpIf(gotoWaypoint, delegate
        {
            var waypoints = learningBoard.GetWaypointsInFrontOfBoard(pawn);
            job.SetTarget(TargetIndex.B, waypoints.RandomElement());
            return true;
        });
    }

    public override void Notify_Starting()
    {
        base.Notify_Starting();
        var learningBoard = job.GetTarget(TargetIndex.A).Thing;
        var waypoints = learningBoard.GetWaypointsInFrontOfBoard(pawn);
        if (waypoints.Count > 0)
        {
            pawn.jobs.curJob.targetB = waypoints[0];
        }
    }

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        return pawn.Reserve(job.GetTarget(TargetIndex.A), job, 1,
            -1, null, errorOnFailed);
    }
}