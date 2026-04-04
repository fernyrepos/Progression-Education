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
            return new Toil
            {
                initAction = delegate
                {
                    ticksUntilNextAction = Rand.Range(60, 120);
                    InitializeWeapon();
                },
                tickIntervalAction = DoLearningInterval,
                defaultCompleteMode = ToilCompleteMode.Never,
                socialMode = RandomSocialMode.Off,
            };
        }

        public override void InitializeWeapon()
        {
            var comp = TargetA.Thing.TryGetComp<CompWeaponTrainingBench>();
            var weaponToUse = comp.WeaponDef;
            weapon = ThingMaker.MakeThing(weaponToUse, GenStuff.DefaultStuffFor(weaponToUse));
        }

        private void DoLearningInterval(int delta)
        {
            if (pawn.GetLord()?.LordJob is not LordJob_AttendClass lordJob
                || !lordJob.studyGroup.ClassIsActive())
            {
                return;
            }
            if (pawn.pather.Moving)
            {
                return;
            }

            lordJob.studyGroup.subjectLogic.ApplyLearningTick(pawn, delta);

            pawn.rotationTracker.FaceCell(TargetA.Cell);

            ticksUntilNextAction -= delta;

            if (ticksUntilNextAction <= 0)
            {
                var thingCell = TargetA.Thing.DrawPos.ToIntVec3();
                var interactionCell = TargetA.Thing.InteractionCell;
                var direction = (interactionCell - thingCell).ToVector3();
                var targetCell = thingCell - direction.ToIntVec3();
                FireProjectile(pawn, weapon, targetCell);
                ticksUntilNextAction = Rand.Range(60, 120);
            }
        }

        private static void FireProjectile(Pawn caster, Thing eq, IntVec3 targetCell)
        {
            var verbProps = eq.TryGetComp<CompEquippable>()?.PrimaryVerb?.verbProps;
            var targetPos = targetCell.ToVector3Shifted();
            Vector3 vector = targetPos
                + RandomHorizontalOffset(
                    caster,
                    targetCell,
                    1f - caster.skills.GetSkill(SkillDefOf.Shooting).Level / 20f
            );
            vector.y = caster.DrawPos.y;

            ThingDef projectileDef = verbProps.defaultProjectile;
            Thing projectile = ThingMaker.MakeThing(projectileDef);
            GenSpawn.Spawn(projectile, caster.Position, caster.Map, Rot4.Random);

            if (verbProps.muzzleFlashScale > 0.01f)
            {
                FleckMaker.Static(caster.Position, caster.Map, FleckDefOf.ShotFlash, verbProps.muzzleFlashScale);
            }
            verbProps.soundCast?.PlayOneShot(new TargetInfo(caster.Position, caster.MapHeld));
            verbProps.soundCastTail?.PlayOneShotOnCamera(caster.Map);

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
