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

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref ticksUntilNextAction, "ticksUntilNextAction", 0);
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
                if (lordJob == null || !lordJob.studyGroup.ClassIsActive())
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
                    InitializeWeapon();

                    var cell = TargetA.Thing.DrawPos.ToIntVec3();
                    FireProjectile(pawn, weapon, cell);
                    ticksUntilNextAction = Rand.Range(60, 120);
                }
            };

            learningToil.defaultCompleteMode = ToilCompleteMode.Never;
            learningToil.socialMode = RandomSocialMode.Off;
            return learningToil;
        }

        public override void InitializeWeapon()
        {
            var comp = TargetA.Thing.TryGetComp<CompWeaponTrainingBench>();
            var weaponToUse = comp.WeaponDef;
            weapon = ThingMaker.MakeThing(weaponToUse, GenStuff.DefaultStuffFor(weaponToUse));
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
