using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace ProgressionEducation
{
    public class QualityBonusRecord
    {
        public QualityCategory quality;
        public float bonus;

        public void LoadDataFromXmlCustom(System.Xml.XmlNode xmlRoot)
        {
            quality = ParseHelper.FromString<QualityCategory>(xmlRoot.Name);
            bonus = ParseHelper.FromString<float>(xmlRoot.InnerText);
        }
    }


    public class CompProperties_LearningBoard : CompProperties
    {
        public List<QualityBonusRecord> qualityBonuses = [];
        public CompProperties_LearningBoard()
        {
            compClass = typeof(CompLearningBoard);
        }

        public float GetQualityBonus(QualityCategory quality)
        {
            var bonus = qualityBonuses.FirstOrDefault(qb => qb.quality == quality);
            return bonus?.bonus ?? 1f;
        }
    }
    public class CompLearningBoard : ThingComp
    {
        public CompProperties_LearningBoard Props => (CompProperties_LearningBoard)props;
        public Classroom classroom;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Deep.Look(ref classroom, "classroom");
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (this.parent.BeingTransportedOnGravship) return;
            
            if (!respawningAfterLoad || classroom == null)
            {
                InitializeClassroom();
            }
            EducationManager.Instance.AddClassroom(classroom);
        }

        public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
        {
            base.PostDeSpawn(map, mode);
            if (this.parent.BeingTransportedOnGravship) return;

            MoveOrRemoveClassroom(map);
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            base.PostDestroy(mode, previousMap);
            MoveOrRemoveClassroom(previousMap);
        }

        private void MoveOrRemoveClassroom(Map map)
        {
            if (classroom is null) return;
            var room = parent.Position.GetRoom(map);
            var otherBoard = room.ContainedThings(parent.def)
                                 .Select(t => t.TryGetComp<CompLearningBoard>())
                                 .FirstOrDefault(c => c != null && c != this);

            if (otherBoard != null)
            {
                EducationLog.Message($"Learning board '{parent.Label}' despawned. Transferring classroom '{classroom.name}' to '{otherBoard.parent}'.");
                otherBoard.classroom = classroom;
            }
            else
            {
                EducationLog.Message($"Learning board '{parent.Label}' despawned. Last board in room. Removing classroom '{classroom.name}'.");
                EducationManager.Instance.RemoveClassroom(classroom);
            }
            classroom = null;
        }

        public void InitializeClassroom()
        {
            if (parent.Faction != Faction.OfPlayer)
            {
                return;
            }

            var room = parent.GetRoom();
            var otherBoard = room.ContainedThings(parent.def)
                                 .Select(t => t.TryGetComp<CompLearningBoard>())
                                 .FirstOrDefault(c => c != null && c != this && c.classroom != null);
 
             if (otherBoard != null)
            {
                classroom = otherBoard.classroom;
                EducationLog.Message($"Learning board '{parent.Label}' spawned in room with existing classroom. Linking to '{classroom.name}'.");
            }
            else
            {
                classroom = new Classroom(parent);
                EducationLog.Message($"Learning board '{parent.Label}' spawned in a new room. Creating new classroom: '{classroom.name}'.");
                Find.WindowStack.Add(new Dialog_RenameClassroom(classroom, true));
            }
        }

        public override string CompInspectStringExtra()
        {
            if (classroom != null)
            {
                return $"{"PE_Classroom".Translate()} {classroom.name}";
            }
            return base.CompInspectStringExtra();
        }
    }
}
