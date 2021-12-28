using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Random = System.Random;

namespace CombatExtended;

internal static class CE_Utility
{
    private const int blitMaxDimensions = 64;

    public const float gravityConst = 9.8f;

    public static List<ThingDef> allWeaponDefs = new List<ThingDef>();

    private static Texture2D Blit(this Texture2D texture, Rect blitRect, int[] rtSize)
    {
        var filterMode = texture.filterMode;
        texture.filterMode = FilterMode.Point;
        var temporary = RenderTexture.GetTemporary(rtSize[0], rtSize[1], 0, RenderTextureFormat.Default,
            RenderTextureReadWrite.Default, 1);
        temporary.filterMode = FilterMode.Point;
        RenderTexture.active = temporary;
        Graphics.Blit(texture, temporary);
        var texture2D = new Texture2D((int)blitRect.width, (int)blitRect.height);
        texture2D.ReadPixels(blitRect, 0, 0);
        texture2D.Apply();
        RenderTexture.active = null;
        texture.filterMode = filterMode;
        return texture2D;
    }

    public static Color[] GetColorSafe(this Texture2D texture, out int width, out int height)
    {
        width = texture.width;
        height = texture.height;
        if (texture.width > texture.height)
        {
            width = Math.Min(width, blitMaxDimensions);
            height = (int)(width * (texture.height / (float)texture.width));
        }
        else if (texture.height > texture.width)
        {
            height = Math.Min(height, blitMaxDimensions);
            width = (int)(height * (texture.width / (float)texture.height));
        }
        else
        {
            width = Math.Min(width, blitMaxDimensions);
            height = Math.Min(height, blitMaxDimensions);
        }

        var blitRect = new Rect(0f, 0f, width, height);
        var rtSize = new[]
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

        var result = texture.Blit(blitRect, rtSize).GetPixels();
        return result;
    }

    public static Texture2D BlitCrop(this Texture2D texture, Rect blitRect)
    {
        return texture.Blit(blitRect, new[]
        {
            texture.width,
            texture.height
        });
    }

    public static Vector2 GenRandInCircle(float radius)
    {
        var random = new Random();
        var num = random.NextDouble() * 3.1415926535897931 * 2.0;
        var num2 = Math.Sqrt(random.NextDouble()) * radius;
        return new Vector2((float)(num2 * Math.Cos(num)), (float)(num2 * Math.Sin(num)));
    }

    public static float GetMoveSpeed(Pawn pawn)
    {
        var num = 60f / pawn.GetStatValue(StatDefOf.MoveSpeed, false);
        num += pawn.Map.pathing.For(pawn).pathGrid.CalculatedCostAt(pawn.Position, false, pawn.Position);
        var edifice = pawn.Position.GetEdifice(pawn.Map);
        if (edifice != null)
        {
            num += edifice.PathWalkCostFor(pawn);
        }

        if (pawn.CurJob == null)
        {
            return 60f / num;
        }

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

        return 60f / num;
    }

    public static float ClosestDistBetween(Vector2 origin, Vector2 destination, Vector2 target)
    {
        return Mathf.Abs(((destination.y - origin.y) * target.x) - ((destination.x - origin.x) * target.y) +
            (destination.x * origin.y) - (destination.y * origin.x)) / (destination - origin).magnitude;
    }

    public static Pawn TryGetTurretOperator(Thing thing)
    {
        if (thing is not Building_Turret)
        {
            return null;
        }

        var compMannable = thing.TryGetComp<CompMannable>();
        return compMannable?.ManningPawn;
    }

    public static bool HasAmmo(this ThingWithComps gun)
    {
        var compAmmoUser = gun.TryGetComp<CompAmmoUser>();
        return compAmmoUser == null || !compAmmoUser.UseAmmo || compAmmoUser.CurMagCount > 0 ||
               compAmmoUser.HasAmmo;
    }

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

        var hediffComp_Stabilize = hediffWithComps.TryGetComp<HediffComp_Stabilize>();
        return hediffComp_Stabilize is { Stabilized: false };
    }

    public static void ThrowEmptyCasing(Vector3 loc, Map map, ThingDef casingMoteDef, float size = 1f)
    {
        if (!Controller.settings.ShowCasings || !loc.ShouldSpawnMotesAt(map) ||
            map.moteCounter.SaturatedLowPriority)
        {
            return;
        }

        var moteThrown = (MoteThrown)ThingMaker.MakeThing(casingMoteDef);
        moteThrown.Scale = Rand.Range(0.5f, 0.3f) * size;
        moteThrown.exactRotation = Rand.Range(-3f, 4f);
        moteThrown.exactPosition = loc;
        moteThrown.airTimeLeft = 60f;
        moteThrown.SetVelocity(Rand.Range(160, 200), Rand.Range(0.7f, 0.5f));
        GenSpawn.Spawn(moteThrown, loc.ToIntVec3(), map);
    }

    public static Bounds GetBoundsFor(IntVec3 cell, RoofDef roof)
    {
        if (roof == null)
        {
            return default;
        }

        var num = 2f;
        if (roof.isNatural)
        {
            num *= 2f;
        }

        if (roof.isThickRoof)
        {
            num *= 2f;
        }

        num = Mathf.Max(0.1f, num - 2f);
        var center = cell.ToVector3Shifted();
        center.y = 2f + (num / 2f);
        return new Bounds(center, new Vector3(1f, num, 1f));
    }

    public static Bounds GetBoundsFor(Thing thing)
    {
        if (thing == null)
        {
            return default;
        }

        var collisionVertical = new CollisionVertical(thing);
        var collisionWidth = GetCollisionWidth(thing);
        var drawPos = thing.DrawPos;
        drawPos.y = collisionVertical.Max - (collisionVertical.HeightRange.Span / 2f);
        return new Bounds(drawPos, new Vector3(collisionWidth, collisionVertical.HeightRange.Span, collisionWidth));
    }

    private static float GetCollisionWidth(Thing thing)
    {
        if (thing is Pawn pawn)
        {
            return GetCollisionBodyFactors(pawn).x;
        }

        return 1f;
    }

    private static Vector2 GetCollisionBodyFactors(Pawn pawn)
    {
        if (pawn == null)
        {
            Log.Error("CE calling GetCollisionBodyHeightFactor with nullPawn");
            return new Vector2(1f, 1f);
        }

        var result = BoundsInjector.ForPawn(pawn);
        if (pawn.GetPosture() == PawnPosture.Standing)
        {
            return result;
        }

        var bodyShape = (pawn.def.GetModExtension<RacePropertiesExtensionCE>() ?? new RacePropertiesExtensionCE())
            .bodyShape;
        if (bodyShape == CE_BodyShapeDefOf.Invalid)
        {
            Log.ErrorOnce("CE returning BodyType Undefined for pawn " + pawn, 35000198 + pawn.GetHashCode());
        }

        result.x *= bodyShape.widthLaying / bodyShape.width;
        result.y *= bodyShape.heightLaying / bodyShape.height;
        return result;
    }

    public static bool IsCrouching(this Pawn pawn)
    {
        if (!pawn.RaceProps.Humanlike || pawn.Downed)
        {
            return false;
        }

        var curJob = pawn.CurJob;
        bool? crouchJob;
        if (curJob == null)
        {
            crouchJob = null;
        }
        else
        {
            var modExtension = curJob.def.GetModExtension<JobDefExtensionCE>();
            crouchJob = modExtension != null ? new bool?(modExtension.isCrouchJob) : null;
        }

        return crouchJob ?? false;
    }

    public static bool IsPlant(this Thing thing)
    {
        return thing.def.category == ThingCategory.Plant;
    }

    public static float MaxProjectileRange(float shotHeight, float shotSpeed, float shotAngle, float gravityFactor)
    {
        if (shotHeight < 0.001f)
        {
            return Mathf.Pow(shotSpeed, 2f) / gravityFactor * Mathf.Sin(2f * shotAngle);
        }

        return shotSpeed * Mathf.Cos(shotAngle) / gravityFactor * ((shotSpeed * Mathf.Sin(shotAngle)) +
                                                                   Mathf.Sqrt(Mathf.Pow(
                                                                                  shotSpeed * Mathf.Sin(shotAngle),
                                                                                  2f) +
                                                                              (2f * gravityFactor * shotHeight)));
    }

    private static void TryUpdateInventory(Pawn pawn)
    {
        var compInventory = pawn?.TryGetComp<CompInventory>();
        compInventory?.UpdateInventory();
    }

    public static void TryUpdateInventory(ThingOwner owner)
    {
        object obj;
        if (owner == null)
        {
            obj = null;
        }
        else
        {
            var owner2 = owner.Owner;
            obj = owner2?.ParentHolder;
        }

        if (obj is Pawn pawn)
        {
            TryUpdateInventory(pawn);
        }
    }
}