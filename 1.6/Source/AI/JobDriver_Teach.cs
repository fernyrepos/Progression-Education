using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace ProgressionEducation
{
    [HotSwappable]
    public class JobDriver_Teach : JobDriver_LessonBase
    {
        public SkillDef taughtSkill;
        public int waitingTicks = 0;

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
            if (!(StudyGroup?.ClassIsActive() ?? true))
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
                    waitingTicks = 0;
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
                waitingTicks++;
            };
            waitToil.FailOn(() => StudyGroup == null);
            yield return waitToil;

            yield return Toils_Jump.JumpIf(wanderToil, () => StudyGroup != null && !StudyGroup.ClassIsActive());

            var gotoWaypoint = Toils_Goto.GotoCell(TargetIndex.B, PathEndMode.OnCell);
            yield return gotoWaypoint;
            var teachAtWaypoint = new Toil
            {
                defaultCompleteMode = ToilCompleteMode.Delay,
                defaultDuration = 300,
                initAction = delegate
                {
                    if (StudyGroup.subjectLogic is ProficiencyClassLogic)
                    {
                        pawn.rotationTracker.FaceCell(pawn.Position + learningBoard.Rotation.FacingCell);
                    }
                    else
                    {
                        pawn.rotationTracker.FaceCell(learningBoard.Position);
                    }
                },
                tickAction = DoTeachingTick
            };
            teachAtWaypoint.AddPreInitAction(InitializeWeapon);
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
                if (IsStudentPresentAndAttending(student, studyGroup))
                {
                    studyGroup.subjectLogic.ApplyTeachingTick(student, this);
                }
            }
        }

        private bool IsStudentPresentAndAttending(Pawn student, StudyGroup studyGroup)
        {
            if (student?.Spawned is false || student.Dead || student.Downed)
            {
                return false;
            }
            if (student.Map != studyGroup.Map)
            {
                return false;
            }
            if (student.jobs?.curDriver is not JobDriver_AttendClass)
            {
                return false;
            }
            Thing learningBoard = studyGroup.classroom?.LearningBoard?.parent;
            if (learningBoard != null)
            {
                Room studentRoom = student.GetRoom();
                Room boardRoom = learningBoard.GetRoom();
                if (studentRoom != boardRoom)
                {
                    return false;
                }
            }
            if (studyGroup.ClassIsActive() == false)
            {
                if (student.jobs?.curDriver is JobDriver_AttendClass attendClassDriver)
                {
                    if (attendClassDriver is JobDriver_AttendMeleeClass)
                    {
                        if (!GenAdj.CellsAdjacent8Way(attendClassDriver.TargetA.Thing).Contains(student.Position))
                        {
                            return false;
                        }
                    }
                    else if (student.Position != JobDriver_AttendClass.DeskSpotStudent(attendClassDriver.job.GetTarget(TargetIndex.A).Thing))
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }

            return true;
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
            Scribe_Deep.Look(ref weapon, "weapon");
            Scribe_Values.Look(ref waitingTicks, "waitingTicks", 0);
        }

        public override void InitializeWeapon()
        {
            var studyGroup = StudyGroup;
            if (studyGroup?.subjectLogic is ProficiencyClassLogic proficiencyLogic)
            {
                ThingDef weaponDef = null;
                switch (proficiencyLogic.proficiencyFocus)
                {
                    case ProficiencyLevel.Firearm:
                        weaponDef = DefsOf.PE_Gun_AssaultRifleTraining;
                        break;
                    case ProficiencyLevel.HighTech:
                        weaponDef = DefsOf.PE_Gun_SpacerTraining;
                        break;
                }

                if (weaponDef != null)
                {
                    weapon = ThingMaker.MakeThing(weaponDef, GenStuff.DefaultStuffFor(weaponDef));
                }
            }
        }
    }
}
