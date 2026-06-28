using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Sound;

namespace ProgressionEducation;

[HotSwappable]
public class JobDriver_AttendShootingClass : JobDriver_AttendClass
{
    private int ticksUntilNextAction;

    protected override void DoLearningInterval(int delta)
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
        if (ticksUntilNextAction > 0
            || weapon == null)
        {
            return;
        }

        var thingCell = TargetA.Thing.DrawPos.ToIntVec3();
        var interactionCell = TargetA.Thing.InteractionCell;
        var direction = (interactionCell - thingCell).ToVector3();
        var targetCell = thingCell - direction.ToIntVec3();
        FireProjectile(pawn, weapon, targetCell);
        ticksUntilNextAction = Rand.Range(60, 120);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref ticksUntilNextAction, "ticksUntilNextAction",
            Rand.Range(60, 120));
    }

    private static void FireProjectile(Pawn caster, Thing weapon, IntVec3 targetCell)
    {
        if (weapon.TryGetComp<CompEquippable>()?.PrimaryVerb?.verbProps is not VerbProperties
            verbProps)
        {
            return;
        }

        if (verbProps.defaultProjectile is not ThingDef projectileDef)
        {
            return;
        }

        var targetPos = targetCell.ToVector3Shifted();
        var missRadius = 1f - caster.skills.GetSkill(SkillDefOf.Shooting).Level / 20f;
        var vector = targetPos
                     + RandomHorizontalOffset(caster, targetCell,
                         missRadius);
        vector.y = caster.DrawPos.y;
        if (ThingMaker.MakeThing(projectileDef) is not Projectile projectile)
        {
            return;
        }

        GenSpawn.Spawn(projectile, caster.Position, caster.Map,
            Rot4.Random);

        if (verbProps.muzzleFlashScale > 0.01f)
        {
            FleckMaker.Static(caster.Position, caster.Map,
                FleckDefOf.ShotFlash,
                verbProps.muzzleFlashScale);
        }

        verbProps.soundCast?.PlayOneShot(new TargetInfo(caster.Position,
            caster.MapHeld));
        verbProps.soundCastTail?.PlayOneShotOnCamera(caster.Map);

        var cell = vector.ToIntVec3();
        projectile.Launch(
            caster,
            caster.DrawPos,
            new LocalTargetInfo(cell),
            new LocalTargetInfo(cell),
            ProjectileHitFlags.IntendedTarget
        );
    }

    public override void InitializeWeapon()
    {
        if (TargetA.Thing?.TryGetComp<CompWeaponTrainingBench>()?.WeaponDef is ThingDef weaponDef)
        {
            weapon = ThingMaker.MakeThing(weaponDef,
                GenStuff.DefaultStuffFor(weaponDef));
        }
    }

    protected override Toil MakeLearningToil()
    {
        this.FailOn(() => !GatheringsUtility.PawnCanStartOrContinueGathering(pawn));
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

    private static Vector3 RandomHorizontalOffset(Pawn caster, IntVec3 targetCell, float maxDist)
    {
        var num = Rand.Range(0f, maxDist);
        var angle = (caster.TrueCenter().Yto0() - targetCell.ToVector3Shifted().Yto0()).AngleFlat();
        var y = Rand.Range(angle - 115, angle + 115);
        return Quaternion.Euler(new Vector3(0f, y, 0f)) * Vector3.forward * num;
    }
}