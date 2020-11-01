using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace CombatExtended
{
    // Token: 0x02000082 RID: 130
    internal static class CE_Utility
    {
        // Token: 0x060002D7 RID: 727 RVA: 0x00017EF0 File Offset: 0x000160F0
        public static Texture2D Blit(this Texture2D texture, Rect blitRect, int[] rtSize)
        {
            FilterMode filterMode = texture.filterMode;
            texture.filterMode = FilterMode.Point;
            RenderTexture temporary = RenderTexture.GetTemporary(rtSize[0], rtSize[1], 0, RenderTextureFormat.Default, RenderTextureReadWrite.Default, 1);
            temporary.filterMode = FilterMode.Point;
            RenderTexture.active = temporary;
            Graphics.Blit(texture, temporary);
            Texture2D texture2D = new Texture2D((int)blitRect.width, (int)blitRect.height);
            texture2D.ReadPixels(blitRect, 0, 0);
            texture2D.Apply();
            RenderTexture.active = null;
            texture.filterMode = filterMode;
            return texture2D;
        }

        // Token: 0x060002D8 RID: 728 RVA: 0x00017F60 File Offset: 0x00016160
        public static Color[] GetColorSafe(this Texture2D texture, out int width, out int height)
        {
            width = texture.width;
            height = texture.height;
            if (texture.width > texture.height)
            {
                width = Math.Min(width, 64);
                height = (int)(width * (texture.height / (float)texture.width));
            }
            else if (texture.height > texture.width)
            {
                height = Math.Min(height, 64);
                width = (int)(height * (texture.width / (float)texture.height));
            }
            else
            {
                width = Math.Min(width, 64);
                height = Math.Min(height, 64);
            }
            Color[] result = null;
            Rect blitRect = new Rect(0f, 0f, width, height);
            int[] rtSize = new int[]
            {
                width,
                height
            };
            if (width == texture.width && height == texture.height)
            {
                try
                {
                    return texture.GetPixels();
                }
                catch
                {
                    return texture.Blit(blitRect, rtSize).GetPixels();
                }
            }
            result = texture.Blit(blitRect, rtSize).GetPixels();
            return result;
        }

        // Token: 0x060002D9 RID: 729 RVA: 0x0001806C File Offset: 0x0001626C
        public static Texture2D BlitCrop(this Texture2D texture, Rect blitRect)
        {
            return texture.Blit(blitRect, new int[]
            {
                texture.width,
                texture.height
            });
        }

        // Token: 0x060002DA RID: 730 RVA: 0x00018090 File Offset: 0x00016290
        public static Vector2 GenRandInCircle(float radius)
        {
            System.Random random = new System.Random();
            double num = random.NextDouble() * 3.1415926535897931 * 2.0;
            double num2 = Math.Sqrt(random.NextDouble()) * radius;
            return new Vector2((float)(num2 * Math.Cos(num)), (float)(num2 * Math.Sin(num)));
        }

        // Token: 0x060002DB RID: 731 RVA: 0x000180E4 File Offset: 0x000162E4
        public static float GetMoveSpeed(Pawn pawn)
        {
            float num = 60f / pawn.GetStatValue(StatDefOf.MoveSpeed, false);
            num += pawn.Map.pathGrid.CalculatedCostAt(pawn.Position, false, pawn.Position);
            Building edifice = pawn.Position.GetEdifice(pawn.Map);
            if (edifice != null)
            {
                num += edifice.PathWalkCostFor(pawn);
            }
            if (pawn.CurJob != null)
            {
                switch (pawn.CurJob.locomotionUrgency)
                {
                    case LocomotionUrgency.Amble:
                        num *= 3f;
                        if (num < 60f)
                        {
                            num = 60f;
                        }
                        break;
                    case LocomotionUrgency.Walk:
                        num *= 2f;
                        if (num < 50f)
                        {
                            num = 50f;
                        }
                        break;
                    case LocomotionUrgency.Sprint:
                        num = Mathf.RoundToInt(num * 0.75f);
                        break;
                }
            }
            return 60f / num;
        }

        // Token: 0x060002DC RID: 732 RVA: 0x000181B8 File Offset: 0x000163B8
        public static float ClosestDistBetween(Vector2 origin, Vector2 destination, Vector2 target)
        {
            return Mathf.Abs((destination.y - origin.y) * target.x - (destination.x - origin.x) * target.y + destination.x * origin.y - destination.y * origin.x) / (destination - origin).magnitude;
        }

        // Token: 0x060002DD RID: 733 RVA: 0x00018220 File Offset: 0x00016420
        public static Pawn TryGetTurretOperator(Thing thing)
        {
            if (thing is Building_Turret)
            {
                CompMannable compMannable = thing.TryGetComp<CompMannable>();
                if (compMannable != null)
                {
                    return compMannable.ManningPawn;
                }
            }
            return null;
        }

        // Token: 0x060002DE RID: 734 RVA: 0x00018248 File Offset: 0x00016448
        public static bool HasAmmo(this ThingWithComps gun)
        {
            CompAmmoUser compAmmoUser = gun.TryGetComp<CompAmmoUser>();
            return compAmmoUser == null || !compAmmoUser.UseAmmo || compAmmoUser.CurMagCount > 0 || compAmmoUser.HasAmmo;
        }

        // Token: 0x060002DF RID: 735 RVA: 0x0001827C File Offset: 0x0001647C
        public static bool CanBeStabilized(this Hediff diff)
        {
            if (!(diff is HediffWithComps hediffWithComps))
            {
                return false;
            }
            if (hediffWithComps.BleedRate == 0f || hediffWithComps.IsTended() || hediffWithComps.IsPermanent())
            {
                return false;
            }
            HediffComp_Stabilize hediffComp_Stabilize = hediffWithComps.TryGetComp<HediffComp_Stabilize>();
            return hediffComp_Stabilize != null && !hediffComp_Stabilize.Stabilized;
        }

        // Token: 0x060002E0 RID: 736 RVA: 0x000182CC File Offset: 0x000164CC
        public static void ThrowEmptyCasing(Vector3 loc, Map map, ThingDef casingMoteDef, float size = 1f)
        {
            if (!Controller.settings.ShowCasings || !loc.ShouldSpawnMotesAt(map) || map.moteCounter.SaturatedLowPriority)
            {
                return;
            }
            MoteThrown moteThrown = (MoteThrown)ThingMaker.MakeThing(casingMoteDef, null);
            moteThrown.Scale = Rand.Range(0.5f, 0.3f) * size;
            moteThrown.exactRotation = Rand.Range(-3f, 4f);
            moteThrown.exactPosition = loc;
            moteThrown.airTimeLeft = 60f;
            moteThrown.SetVelocity(Rand.Range(160, 200), Rand.Range(0.7f, 0.5f));
            GenSpawn.Spawn(moteThrown, loc.ToIntVec3(), map, WipeMode.Vanish);
        }

        // Token: 0x060002E1 RID: 737 RVA: 0x0001837C File Offset: 0x0001657C
        public static Bounds GetBoundsFor(IntVec3 cell, RoofDef roof)
        {
            if (roof == null)
            {
                return default;
            }
            float num = 2f;
            if (roof.isNatural)
            {
                num *= 2f;
            }
            if (roof.isThickRoof)
            {
                num *= 2f;
            }
            num = Mathf.Max(0.1f, num - 2f);
            Vector3 center = cell.ToVector3Shifted();
            center.y = 2f + num / 2f;
            return new Bounds(center, new Vector3(1f, num, 1f));
        }

        // Token: 0x060002E2 RID: 738 RVA: 0x00018400 File Offset: 0x00016600
        public static Bounds GetBoundsFor(Thing thing)
        {
            if (thing == null)
            {
                return default;
            }
            CollisionVertical collisionVertical = new CollisionVertical(thing);
            float collisionWidth = GetCollisionWidth(thing);
            Vector3 drawPos = thing.DrawPos;
            drawPos.y = collisionVertical.Max - collisionVertical.HeightRange.Span / 2f;
            return new Bounds(drawPos, new Vector3(collisionWidth, collisionVertical.HeightRange.Span, collisionWidth));
        }

        // Token: 0x060002E3 RID: 739 RVA: 0x00018474 File Offset: 0x00016674
        public static float GetCollisionWidth(Thing thing)
        {
            if (thing is Pawn pawn)
            {
                return GetCollisionBodyFactors(pawn).x;
            }
            return 1f;
        }

        // Token: 0x060002E4 RID: 740 RVA: 0x0001849C File Offset: 0x0001669C
        public static Vector2 GetCollisionBodyFactors(Pawn pawn)
        {
            if (pawn == null)
            {
                Log.Error("CE calling GetCollisionBodyHeightFactor with nullPawn", false);
                return new Vector2(1f, 1f);
            }
            Vector2 result = BoundsInjector.ForPawn(pawn);
            if (pawn.GetPosture() != PawnPosture.Standing)
            {
                BodyShapeDef bodyShape = (pawn.def.GetModExtension<RacePropertiesExtensionCE>() ?? new RacePropertiesExtensionCE()).bodyShape;
                if (bodyShape == CE_BodyShapeDefOf.Invalid)
                {
                    Log.ErrorOnce("CE returning BodyType Undefined for pawn " + pawn.ToString(), 35000198 + pawn.GetHashCode(), false);
                }
                result.x *= bodyShape.widthLaying / bodyShape.width;
                result.y *= bodyShape.heightLaying / bodyShape.height;
            }
            return result;
        }

        // Token: 0x060002E5 RID: 741 RVA: 0x0001854C File Offset: 0x0001674C
        public static bool IsCrouching(this Pawn pawn)
        {
            if (pawn.RaceProps.Humanlike && !pawn.Downed)
            {
                Job curJob = pawn.CurJob;
                bool? flag;
                if (curJob == null)
                {
                    flag = null;
                }
                else
                {
                    JobDefExtensionCE modExtension = curJob.def.GetModExtension<JobDefExtensionCE>();
                    flag = ((modExtension != null) ? new bool?(modExtension.isCrouchJob) : null);
                }
                return flag ?? false;
            }
            return false;
        }

        // Token: 0x060002E6 RID: 742 RVA: 0x000185BB File Offset: 0x000167BB
        public static bool IsPlant(this Thing thing)
        {
            return thing.def.category == ThingCategory.Plant;
        }

        // Token: 0x060002E7 RID: 743 RVA: 0x000185CC File Offset: 0x000167CC
        public static float MaxProjectileRange(float shotHeight, float shotSpeed, float shotAngle, float gravityFactor)
        {
            if (shotHeight < 0.001f)
            {
                return Mathf.Pow(shotSpeed, 2f) / gravityFactor * Mathf.Sin(2f * shotAngle);
            }
            return shotSpeed * Mathf.Cos(shotAngle) / gravityFactor * (shotSpeed * Mathf.Sin(shotAngle) + Mathf.Sqrt(Mathf.Pow(shotSpeed * Mathf.Sin(shotAngle), 2f) + 2f * gravityFactor * shotHeight));
        }

        // Token: 0x060002E8 RID: 744 RVA: 0x00018634 File Offset: 0x00016834
        public static void TryUpdateInventory(Pawn pawn)
        {
            if (pawn != null)
            {
                CompInventory compInventory = pawn.TryGetComp<CompInventory>();
                if (compInventory != null)
                {
                    compInventory.UpdateInventory();
                }
            }
        }

        // Token: 0x060002E9 RID: 745 RVA: 0x00018654 File Offset: 0x00016854
        public static void TryUpdateInventory(ThingOwner owner)
        {
            object obj;
            if (owner == null)
            {
                obj = null;
            }
            else
            {
                IThingHolder owner2 = owner.Owner;
                obj = (owner2?.ParentHolder);
            }
            if (obj is Pawn pawn)
            {
                TryUpdateInventory(pawn);
            }
        }

        // Token: 0x0400021E RID: 542
        private const int blitMaxDimensions = 64;

        // Token: 0x0400021F RID: 543
        public static List<ThingDef> allWeaponDefs = new List<ThingDef>();

        // Token: 0x04000220 RID: 544
        public const float gravityConst = 9.8f;
    }
}
