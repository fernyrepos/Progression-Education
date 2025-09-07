using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace ProgressionEducation
{
    public class Classroom : IExposable, ILoadReferenceable, IRenameable
    {
        public int id;
        public string name;
        public Color color;
        private Thing learningBoardThing;
        public CompLearningBoard LearningBoard => learningBoardThing.TryGetComp<CompLearningBoard>();
        public int participantCount;

        public Classroom()
        {
        }

        public Classroom(Thing board)
        {
            learningBoardThing = board;
            var educationManager = EducationManager.Instance;
            id = educationManager.GetNextClassroomId();
            name = "PE_Classroom".Translate() + " " + (educationManager.Classrooms.Count + 1);
            color = new Color(0.5f, 0.5f, 1f);
            participantCount = 0;
        }

        public void SetParticipantCount(int count)
        {
            participantCount = count;
        }

        public float CalculateLearningModifier()
        {
            var room = learningBoardThing.GetRoom();
            FindEducationalFacilities(room, out var learningBoards, out var projectors);
            float bestBoardBonus = CalculateBestBoardBonus(learningBoards);
            float totalProjectorBonus = CalculateTotalProjectorBonus(projectors);
            return bestBoardBonus * (1f + totalProjectorBonus);
        }

        private void FindEducationalFacilities(Room room, out List<Thing> boards, out List<CompProjector> projectors)
        {
            boards = [];
            projectors = [];

            foreach (var thing in room.ContainedAndAdjacentThings)
            {
                var learningBoardComp = thing.TryGetComp<CompLearningBoard>();
                if (learningBoardComp != null)
                {
                    boards.Add(thing);
                    var facilities = thing.TryGetComp<CompAffectedByFacilities>();
                    if (facilities != null)
                    {
                        foreach (var facility in facilities.LinkedFacilitiesListForReading)
                        {
                            var projector = facility.TryGetComp<CompProjector>();
                            if (projector != null)
                            {
                                projectors.Add(projector);
                            }
                        }
                    }
                    continue;
                }
                var standaloneProjector = thing.TryGetComp<CompProjector>();
                if (standaloneProjector != null)
                {
                    projectors.Add(standaloneProjector);
                }
            }
        }

        private float CalculateBestBoardBonus(List<Thing> boards)
        {
            float bestBonus = 1f;

            foreach (var boardThing in boards)
            {
                var learningBoard = boardThing.TryGetComp<CompLearningBoard>();
                float qualityBonus = 1f;
                var compQuality = boardThing.TryGetComp<CompQuality>();
                if (compQuality != null)
                {
                    qualityBonus = learningBoard.Props.GetQualityBonus(compQuality.Quality);
                }
                float materialBonus = 1f;
                if (boardThing.Stuff != null)
                {
                    materialBonus = learningBoard.Props.GetTechLevelBonus(boardThing.Stuff.techLevel);
                }
                float boardBonus = qualityBonus * materialBonus;
                bestBonus = Math.Max(bestBonus, boardBonus);
            }

            return bestBonus;
        }

        private float CalculateTotalProjectorBonus(List<CompProjector> projectors)
        {
            float totalBonus = 0.0f;
            foreach (var projector in projectors)
            {
                totalBonus += projector.Props.learningBonus;
            }
            return totalBonus;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref id, "id", 0);
            Scribe_Values.Look(ref name, "name");
            Scribe_Values.Look(ref color, "color");
            Scribe_References.Look(ref learningBoardThing, "learningBoard");
            Scribe_Values.Look(ref participantCount, "participantCount");
        }

        public string GetUniqueLoadID()
        {
            return "Classroom_" + id;
        }

        public string RenamableLabel
        {
            get => name;
            set => name = value;
        }

        public string BaseLabel => name;

        public string InspectLabel => name;
    }
}
