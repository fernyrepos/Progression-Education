using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Sound;

namespace ProgressionEducation
{
    [HotSwappable]
    public class JobDriver_AttendShootingClass : JobDriver_AttendClass
    {
        private int ticksUntilNextAction;
        public Thing weapon;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref ticksUntilNextAction, "ticksUntilNextAction", 0);
            Scribe_Deep.Look(ref weapon, "weapon");
        }

        protected override Toil MakeLearningToil()
        {
            Toil learningToil = new Toil();

            learningToil.initAction = delegate
            {
                ticksUntilNextAction = Rand.Range(60, 120);
            };

            learningToil.tickAction = delegate
            {
                LordJob_AttendClass lordJob = pawn.GetLord()?.LordJob as LordJob_AttendClass;
                if (lordJob == null || !lordJob.studyGroup.AllStudentsAreGathered())
                {
                    return;
                }

                lordJob.studyGroup.subjectLogic.ApplyLearningTick(pawn);

                if (pawn.pather.Moving)
                {
                    return;
                }

                pawn.rotationTracker.FaceCell(TargetA.Cell);

                ticksUntilNextAction--;

                if (ticksUntilNextAction <= 0)
                {
                    if (weapon is null)
                    {
                        weapon = ThingMaker.MakeThing(DefsOf.PE_Gun_AssaultRifleTraining, GenStuff.DefaultStuffFor(DefsOf.PE_Gun_AssaultRifleTraining));
                    }
                    var cell = TargetA.Thing.OccupiedRect().OrderByDescending(c => c.DistanceToSquared(pawn.Position)).FirstOrDefault();
                    FireProjectile(pawn, weapon, cell);
                    ticksUntilNextAction = Rand.Range(60, 120);
                }
            };

            learningToil.defaultCompleteMode = ToilCompleteMode.Never;
            learningToil.socialMode = RandomSocialMode.Off;
            return learningToil;
        }

        public void DrawEquipment(Vector3 rootLoc, Rot4 pawnRotation, PawnRenderFlags flags)
        {
            if (pawn.pather.Moving)
            {
                return;
            }
            Vector3 drawLoc = new Vector3(0f, (pawnRotation == Rot4.North) ? (-0.00289575267f) : 0.03474903f, 0f);
            Vector3 vector = TargetA.Thing.DrawPos;
            float num = 0f;
            if ((vector - pawn.DrawPos).MagnitudeHorizontalSquared() > 0.001f)
            {
                num = (vector - pawn.DrawPos).AngleFlat();
            }
            drawLoc += rootLoc + new Vector3(0f, 0f, 0.4f).RotatedBy(num);
            if (weapon is null)
            {
                weapon = ThingMaker.MakeThing(DefsOf.PE_Gun_AssaultRifleTraining, GenStuff.DefaultStuffFor(DefsOf.PE_Gun_AssaultRifleTraining));
            }
            DrawEquipmentAiming(weapon, drawLoc, num);
        }

        public void DrawEquipmentAiming(Thing eq, Vector3 drawLoc, float aimAngle)
        {
            Mesh mesh = null;
            float num = aimAngle - 90f;
            if (aimAngle > 20f && aimAngle < 160f)
            {
                mesh = MeshPool.plane10;
                num += eq.def.equippedAngleOffset;
            }
            else if (aimAngle > 200f && aimAngle < 340f)
            {
                mesh = MeshPool.plane10Flip;
                num -= 180f;
                num -= eq.def.equippedAngleOffset;
            }
            else
            {
                mesh = MeshPool.plane10;
                num += eq.def.equippedAngleOffset;
            }
            num %= 360f;
            CompEquippable compEquippable = eq.TryGetComp<CompEquippable>();
            if (compEquippable != null)
            {
                EquipmentUtility.Recoil(eq.def, EquipmentUtility.GetRecoilVerb(compEquippable.AllVerbs), out var drawOffset, out var angleOffset, aimAngle);
                drawLoc += drawOffset;
                num += angleOffset;
            }
            Graphic_StackCount graphic_StackCount = eq.Graphic as Graphic_StackCount;
            Graphics.DrawMesh(material: (graphic_StackCount == null) ? eq.Graphic.MatSingle : graphic_StackCount.SubGraphicForStackCount(1, eq.def).MatSingle, mesh: mesh, position: drawLoc, rotation: Quaternion.AngleAxis(num, Vector3.up), layer: 0);
        }
        private static void FireProjectile(Pawn caster, Thing eq, IntVec3 targetCell)
        {
            var verbProps = eq.TryGetComp<CompEquippable>()?.PrimaryVerb?.verbProps;
            var targetPos = targetCell.ToVector3Shifted();
            Vector3 vector = targetPos +
                RandomHorizontalOffset(caster, targetCell,
                (1f - (float)caster.skills.GetSkill(SkillDefOf.Shooting).Level / 20f));
            vector.y = caster.DrawPos.y;

            ThingDef projectileDef = verbProps.defaultProjectile;
            Thing projectile = ThingMaker.MakeThing(projectileDef);
            GenSpawn.Spawn(projectile, caster.Position, caster.Map, Rot4.Random);
            
            if (verbProps.muzzleFlashScale > 0.01f)
            {
                FleckMaker.Static(caster.Position, caster.Map, FleckDefOf.ShotFlash, verbProps.muzzleFlashScale);
            }
            if (verbProps.soundCast != null)
            {
                verbProps.soundCast.PlayOneShot(new TargetInfo(caster.Position, caster.MapHeld));
            }
            if (verbProps.soundCastTail != null)
            {
                verbProps.soundCastTail.PlayOneShotOnCamera(caster.Map);
            }
            
            var cell = vector.ToIntVec3();
            Projectile proj = (Projectile)projectile;
            proj.Launch(caster, caster.DrawPos, new LocalTargetInfo(cell), new LocalTargetInfo(cell), ProjectileHitFlags.IntendedTarget, equipment: null);
        }

        public static Vector3 RandomHorizontalOffset(Pawn caster, IntVec3 targetCell, float maxDist)
        {
            float num = Rand.Range(0f, maxDist);
            var angle = (caster.TrueCenter().Yto0() - targetCell.ToVector3Shifted().Yto0()).AngleFlat();
            float y = Rand.Range(angle - 115, angle + 115);
            return Quaternion.Euler(new Vector3(0f, y, 0f)) * Vector3.forward * num;
        }
    }
}
