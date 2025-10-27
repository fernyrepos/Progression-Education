using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ProgressionEducation
{
    public abstract class JobDriver_LessonBase : JobDriver
    {
        public Thing weapon;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref weapon, "weapon");
        }

        public virtual void InitializeWeapon() { }

        public void DrawEquipment(Vector3 rootLoc, Rot4 pawnRotation, PawnRenderFlags flags)
        {
            if (pawn.pather.Moving || weapon is null)
            {
                return;
            }
            Vector3 drawLoc = new Vector3(0f, (pawnRotation == Rot4.North) ? (-0.00289575267f) : 0.03474903f, 0f);
            Vector3 vector = pawn.DrawPos;
            float num = 0f;
            if ((vector - pawn.DrawPos).MagnitudeHorizontalSquared() > 0.001f)
            {
                num = (vector - pawn.DrawPos).AngleFlat();
            }
            drawLoc += rootLoc + new Vector3(0f, 0f, 0.4f).RotatedBy(num);
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
    }
}
