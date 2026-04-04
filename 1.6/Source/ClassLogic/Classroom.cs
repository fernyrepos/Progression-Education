using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace ProgressionEducation
{
    [HotSwappable]
    public class Classroom : IExposable, ILoadReferenceable, IRenameable
    {
        public int id;
        public string name;
        public Color color;
        private Thing learningBoardThing;
        public CompLearningBoard LearningBoard => learningBoardThing.TryGetComp<CompLearningBoard>();
        public bool restrictReservationsDuringClass = true;
        public bool interruptJobs = false;

        public Classroom()
        {
        }

        public Classroom(Thing board)
        {
            learningBoardThing = board;
            var educationManager = EducationManager.Instance;
            id = educationManager.GetNextClassroomId();
            name = "PE_Classroom".Translate() + " " + (educationManager.Classrooms.Count + 1);
            color = new Color(Rand.Value, Rand.Value, Rand.Value);
        }

        public float CalculateLearningModifier()
        {
            FindEducationalFacilities(out var projectors, out var schoolDesks);
            var projectorOffset = projectors.Sum(p => p.Props.learningBonus);
            return 1f + projectorOffset;
        }

        private void FindEducationalFacilities(out List<CompProjector> projectors, out List<CompSchoolDesk> schoolDesks)
        {
            projectors = [];
            schoolDesks = [];

            var facilities = learningBoardThing.TryGetComp<CompAffectedByFacilities>();
            if (facilities != null)
            {
                foreach (var facility in facilities.LinkedFacilitiesListForReading)
                {
                    var projector = facility.TryGetComp<CompProjector>();
                    if (projector is { Active: true })
                    {
                        projectors.Add(projector);
                    }
                    var desk = facility.TryGetComp<CompSchoolDesk>();
                    if (desk != null)
                    {
                        schoolDesks.Add(desk);
                    }
                }
            }
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref id, "id", 0);
            Scribe_Values.Look(ref name, "name");
            Scribe_Values.Look(ref color, "color");
            Scribe_References.Look(ref learningBoardThing, "learningBoard");
            Scribe_Values.Look(ref restrictReservationsDuringClass, "restrictReservationsDuringClass", true);
            Scribe_Values.Look(ref interruptJobs, "interruptJobs");
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
