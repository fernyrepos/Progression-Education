using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace ProgressionEducation
{
    [HotSwappable]
    public class JobDriver_Teach : JobDriver
    {
        public SkillDef taughtSkill;

        private StudyGroup StudyGroup
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

        public override string GetReport()
        {
            if (!(StudyGroup?.AllStudentsAreGathered() ?? true))
            {
                return "PE_JobReport_WaitingForStudents".Translate();
            }
            return base.GetReport();
        }
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.GetTarget(TargetIndex.A), job, 1, -1, null, errorOnFailed);
        }

        public override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);
            this.FailOn(() => pawn.mindState.duty.def != DefsOf.PE_TeachDuty);
            this.FailOn(() => StudyGroup == null || StudyGroup.teacher != pawn);
            var learningBoard = job.GetTarget(TargetIndex.A).Thing;

            Toil wanderToil = new Toil
            {
                initAction = () =>
                {
                    var waypoints = EducationUtility.GetWaypointsInFrontOfBoard(learningBoard, pawn);
                    if (waypoints.Any())
                    {
                        job.SetTarget(TargetIndex.B, waypoints.RandomElement());
                        pawn.pather.StartPath(job.GetTarget(TargetIndex.B), PathEndMode.OnCell);
                    }
                },
                defaultCompleteMode = ToilCompleteMode.PatherArrival
            };
            wanderToil.FailOn(() => StudyGroup == null);
            yield return wanderToil;

            Toil waitToil = Toils_General.Wait(250);
            waitToil.handlingFacing = true;
            waitToil.socialMode = RandomSocialMode.Normal;
            waitToil.tickAction = delegate
            {
                PawnUtility.GainComfortFromCellIfPossible(pawn, 1, true);
                pawn.rotationTracker.FaceCell(pawn.Position + learningBoard.Rotation.FacingCell);
            };
            waitToil.FailOn(() => StudyGroup == null);
            yield return waitToil;

            yield return Toils_Jump.JumpIf(wanderToil, () => StudyGroup != null && !StudyGroup.AllStudentsAreGathered());

            var gotoWaypoint = Toils_Goto.GotoCell(TargetIndex.B, PathEndMode.OnCell);
            gotoWaypoint.tickAction = DoTeachingTick;
            yield return gotoWaypoint;
            var teachAtWaypoint = new Toil
            {
                defaultCompleteMode = ToilCompleteMode.Delay,
                defaultDuration = 300,
                initAction = () => pawn.rotationTracker.FaceCell(learningBoard.Position),
                tickAction = DoTeachingTick
            };
            yield return teachAtWaypoint;
            yield return Toils_Jump.JumpIf(gotoWaypoint, delegate
            {
                var waypoints = EducationUtility.GetWaypointsInFrontOfBoard(learningBoard, pawn);
                job.SetTarget(TargetIndex.B, waypoints.RandomElement());
                return true;
            });
        }
        private void DoTeachingTick()
        {
            var studyGroup = StudyGroup;
            PawnUtility.GainComfortFromCellIfPossible(pawn, 1, true);
            pawn.skills.Learn(SkillDefOf.Social, 0.1f);

            if (studyGroup.subjectLogic.IsInfinite is false)
            {
                float semesterProgress = studyGroup.CalculateProgressPerTick();
                studyGroup.AddProgress(semesterProgress);
            }

            foreach (var student in studyGroup.students)
            {
                studyGroup.subjectLogic.ApplyTeachingTick(student, this);
            }
        }

        public override void Notify_Starting()
        {
            base.Notify_Starting();
            var learningBoard = job.GetTarget(TargetIndex.A).Thing;

            var waypoints = EducationUtility.GetWaypointsInFrontOfBoard(learningBoard, pawn);
            if (waypoints.Count > 0)
            {
                pawn.jobs.curJob.targetB = waypoints[0];
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look(ref taughtSkill, "taughtSkill");
        }
    }
}
