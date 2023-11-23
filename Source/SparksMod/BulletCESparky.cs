using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CombatEffectsCE;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace CombatExtended;

public class BulletCESparky : ProjectileCE
{
    private readonly bool debugDrawIntercepts = false;
    private float _gravityFactor = -1f;
    private Sustainer ambientSustainer;
    private float energyRemaining = 100f;
    private float heightInt;
    private Vector3 impactPosition;
    private int intTicksToImpact = -1;
    private Vector3 lastExactPos = new Vector3(-1000f, 0f, 0f);
    private int lastHeightTick = -1;

    private Thing lastThingHit;
    public ProjectilePropertiesWithEffectsCE projectileProperties;

    private float startingTicksToImpactInt = -1f;
    private float suppressionAmount;

    public override Vector3 ExactPosition
    {
        get
        {
            if (landed)
            {
                return impactPosition;
            }

            var vector = Vec2Position();
            return new Vector3(vector.x, Height, vector.y);
        }
        set
        {
            impactPosition = new Vector3(value.x, value.y, value.z);
            Position = impactPosition.ToIntVec3();
        }
    }

    public override Vector3 DrawPos
    {
        get
        {
            var drawPosV = DrawPosV2;
            return new Vector3(drawPosV.x, def.Altitude, drawPosV.y);
        }
    }

    private new Vector2 DrawPosV2 => Vec2Position() + new Vector2(0f,
        Height - (shotHeight * ((StartingTicksToImpact - fTicks) / StartingTicksToImpact)));

    private new int FlightTicks => IntTicksToImpact - ticksToImpact;

    private new float fTicks
    {
        get
        {
            if (ticksToImpact != 0)
            {
                return FlightTicks;
            }

            return StartingTicksToImpact;
        }
    }

    private new int IntTicksToImpact
    {
        get
        {
            if (intTicksToImpact < 0)
            {
                intTicksToImpact = Mathf.CeilToInt(StartingTicksToImpact);
            }

            return intTicksToImpact;
        }
    }

    private new float StartingTicksToImpact
    {
        get
        {
            if (!(startingTicksToImpactInt < 0f))
            {
                return startingTicksToImpactInt;
            }

            if (shotHeight < 0.001f)
            {
                if (shotAngle < 0f)
                {
                    destinationInt = origin;
                    startingTicksToImpactInt = 0f;

                    CombatEffectsCEMod.LogMessage($"{def.LabelCap} has negative height and angle, impacting");
                    ImpactSomething();
                    return 0f;
                }

                startingTicksToImpactInt =
                    (origin - Destination).magnitude / (Mathf.Cos(shotAngle) * shotSpeed) * 60f;
                return startingTicksToImpactInt;
            }

            startingTicksToImpactInt = GetFlightTime() * 60f;

            CombatEffectsCEMod.LogMessage($"{def.LabelCap} has {startingTicksToImpactInt} starting ticks to impact");
            return startingTicksToImpactInt;
        }
    }

    private new float Height
    {
        get
        {
            if (lastHeightTick == FlightTicks)
            {
                return heightInt;
            }

            heightInt = ticksToImpact > 0 ? GetHeightAtTicks(FlightTicks) : 0f;
            lastHeightTick = FlightTicks;
            return heightInt;
        }
    }

    private float GravityFactor
    {
        get
        {
            if (!(_gravityFactor < 0f))
            {
                return _gravityFactor;
            }

            _gravityFactor = 1.96f;
            if (def.projectile is ProjectilePropertiesCE projectilePropertiesCE)
            {
                _gravityFactor = projectilePropertiesCE.Gravity;
            }

            return _gravityFactor;
        }
    }

    private Vector3 LastPos
    {
        get
        {
            if (!(lastExactPos.x < -999f))
            {
                return lastExactPos;
            }

            var vector = Vec2Position(FlightTicks - 1);
            lastExactPos = new Vector3(vector.x, GetHeightAtTicks(FlightTicks - 1), vector.y);
            return lastExactPos;
        }
        set => lastExactPos = value;
    }

    private void ApplySuppression(Pawn pawn)
    {
        CompShield shieldBelt = null;
        if (pawn.RaceProps.Humanlike)
        {
            var wornApparel = pawn.apparel.WornApparel;
            foreach (var apparel in wornApparel)
            {
                if (!apparel.def.IsShieldThatBlocksRanged)
                {
                    continue;
                }

                shieldBelt = apparel.GetComp<CompShield>();
                break;
            }
        }

        var compSuppressable = pawn.TryGetComp<CompSuppressable>();
        if (compSuppressable == null)
        {
            return;
        }

        var faction = pawn.Faction;
        var thing = launcher;
        if (faction == thing?.Faction || shieldBelt != null &&
            shieldBelt.ShieldState != ShieldState.Resetting)
        {
            return;
        }

        suppressionAmount = def.projectile.GetDamageAmount(1f);
        var num = def.projectile is not ProjectilePropertiesCE projectilePropertiesCE
            ? 0f
            : projectilePropertiesCE.GetArmorPenetration(1f);
        suppressionAmount *=
            1f - Mathf.Clamp(pawn.GetStatValue(CE_StatDefOf.AverageSharpArmor) * 0.5f / num, 0f, 1f);
        compSuppressable.AddSuppression(suppressionAmount, OriginIV3);
    }

    private void LogImpact(Thing hitThing, out LogEntry_DamageResult logEntry)
    {
        var targetThing = (Thing)intendedTarget.Pawn;
        if (targetThing == null)
        {
            targetThing = intendedTarget.Thing;
        }

        logEntry = new BattleLogEntry_RangedImpact(launcher, hitThing, targetThing, equipmentDef, def, null);
        if (launcher is not AmmoThing)
        {
            Find.BattleLog.Add(logEntry);
        }
    }

    public override void Impact(Thing hitThing)
    {
        // Okay this is not entirely good
        // I removed the timer since it's not really needed.
        // But when ticksToImpact is zero it can still hit the target a second time!
        if (hitThing == lastThingHit && ticksToImpact > 0 && impactPosition.y > 0)
        {
            //After penetration the bullet can hit the same target multiple times. This tries to prevent that.
            return;
        }

        lastThingHit = hitThing;

        var map = Map;
        LogEntry_DamageResult logEntry_DamageResult = null;

        var isDeflectedByPawn = false;

        if ((logMisses || !logMisses && hitThing != null && hitThing is Pawn or Building_Turret) &&
            launcher is not AmmoThing)
        {
            LogImpact(hitThing, out logEntry_DamageResult);
        }

        var skipSound = false;

        if (hitThing != null)
        {
            var angleY =
                ExactRotation.eulerAngles
                    .y; // This is the bullet heading angle in respect to the north vector around the up vector

            if (hitThing is Building)
            {
                var angle = angleY - 180f;

                if (hitThing.def.MadeFromStuff)
                {
                    if (hitThing.Stuff.label == "wood" && projectileProperties?.effectWoodWallHit != null
                       ) // Wooden wall
                    {
                        projectileProperties.effectWoodWallHit.children[0].angle =
                            new FloatRange(angle - 90f, angle + 90f);
                        var eff = projectileProperties.effectWoodWallHit.Spawn();
                        eff.Trigger(hitThing, hitThing);
                    }
                    else if (projectileProperties?.effectWoodWallHit != null) // Non-Wood Wall
                    {
                        projectileProperties.effectStoneWallHit.children[1].angle =
                            new FloatRange(angle - 90f, angle + 90f);
                        var eff = projectileProperties.effectStoneWallHit.Spawn();
                        eff.Trigger(hitThing, hitThing);
                    }
                }
                else if (projectileProperties?.effectStoneWallHit != null) //Natural Stone
                {
                    projectileProperties.effectStoneWallHit.children[1].angle =
                        new FloatRange(angle - 90f, angle + 90f);
                    var eff = projectileProperties.effectStoneWallHit.Spawn();
                    eff.Trigger(hitThing, hitThing);
                }


                if (hitThing.def.MadeFromStuff)
                {
                    var hitThingColor = hitThing.Stuff.graphic.color;
                    hitThingColor.a = 0.6f;
                    if (projectileProperties?.effectBuildingBits.children[0].moteDef.graphicData != null)
                    {
                        projectileProperties.effectBuildingBits.children[0].moteDef.graphicData.color = hitThingColor;
                    }
                }

                if (projectileProperties?.effectBuildingBits.children[0] != null)
                {
                    projectileProperties.effectBuildingBits.children[0].angle =
                        new FloatRange(angle - 45f, angle + 45f);
                }

                var effecter = projectileProperties?.effectBuildingBits.Spawn();
                effecter?.Trigger(hitThing, hitThing);

                effecter = projectileProperties?.effectPuff.Spawn();
                effecter?.Trigger(this, hitThing);
            }

            var damageAmount = def.projectile.GetDamageAmount(1f);
            var damageDefExtensionCE = def.projectile.damageDef.GetModExtension<DamageDefExtensionCE>() ??
                                       new DamageDefExtensionCE();
            var projectilePropertiesCE = (ProjectilePropertiesCE)def.projectile;
            var armorPenetration = def.projectile.damageDef.armorCategory != DamageArmorCategoryDefOf.Sharp
                ? projectilePropertiesCE.armorPenetrationBlunt
                : projectilePropertiesCE.armorPenetrationSharp;
            var dinfo = new DamageInfo(def.projectile.damageDef, damageAmount, armorPenetration,
                ExactRotation.eulerAngles.y, launcher, null, def);
            var depth = damageDefExtensionCE.harmOnlyOutsideLayers
                ? BodyPartDepth.Outside
                : BodyPartDepth.Undefined;
            var collisionBodyHeight = new CollisionVertical(hitThing).GetCollisionBodyHeight(ExactPosition.y);
            dinfo.SetBodyRegion(collisionBodyHeight, depth);
            if (damageDefExtensionCE.harmOnlyOutsideLayers)
            {
                dinfo.SetBodyRegion(BodyPartHeight.Undefined, BodyPartDepth.Outside);
            }

            if (launcher is AmmoThing && hitThing is Pawn pawn)
            {
                logEntry_DamageResult = new BattleLogEntry_DamageTaken(pawn,
                    DefDatabase<RulePackDef>.GetNamed("DamageEvent_CookOff"));
                Find.BattleLog.Add(logEntry_DamageResult);
            }

            var dmgRes = hitThing.TakeDamage(dinfo);
            CombatEffectsCEMod.LogMessage($"{def.LabelCap} is it deflected? : {dmgRes.deflected}");
            if (CombatEffectsCEMod.instance.Settings.ExtraBlood && !dmgRes.deflected && hitThing is Pawn &&
                projectileProperties?.effectBloodHit != null)
            {
                CombatEffectsCEMod.LogMessage($"{def.LabelCap} Hit someone!");
                CombatEffectsCEMod.LogMessage($"{def.LabelCap} Show extra blood");
                if (hitThing.def.race.IsMechanoid ||
                    Main.VehiclesLoaded && hitThing.def.thingClass.Name.EndsWith("VehiclePawn"))
                {
                    CombatEffectsCEMod.LogMessage($"{hitThing.def} is not a humanoid, using oil color");
                    var bloodColor = new Color(0.04f, 0.04f, 0.04f, 0.7f);
                    projectileProperties.effectBloodHit.children[0].moteDef.graphicData.color = bloodColor;
                    ((MyGraphicData)((MotePropertiesFilthy)projectileProperties.effectBloodHit.children[0].moteDef
                        .mote).filthTrace.graphicData).ChangeGraphicColor(bloodColor);
                }
                else
                {
                    var bloodColor = hitThing.def.race.meatColor;
                    if (bloodColor == Color.white && hitThing.def.race.BloodDef?.graphicData?.color != null)
                    {
                        bloodColor = hitThing.def.race.BloodDef.graphicData.color;
                    }

                    if (bloodColor == Color.white)
                    {
                        CombatEffectsCEMod.LogMessage($"{def.LabelCap} Blood color changing!");
                        bloodColor = new Color(0.5f, 0.1f, 0.1f);
                    }

                    var m = $"Requested color : {bloodColor.ToString()}";
                    CombatEffectsCEMod.LogMessage(m);

                    projectileProperties.effectBloodHit.children[0].moteDef.graphicData.color = bloodColor;
                    ((MyGraphicData)((MotePropertiesFilthy)projectileProperties.effectBloodHit.children[0].moteDef
                        .mote).filthTrace.graphicData).ChangeGraphicColor(bloodColor);
                }

                var bloodEffect = ((ProjectilePropertiesWithEffectsCE)def.projectile).effectBloodHit.Spawn();
                bloodEffect.Trigger(hitThing, hitThing);
            }

            isDeflectedByPawn = dmgRes.deflected;

            //Log the result of the hit!
            dmgRes.AssociateWithLog(logEntry_DamageResult);
            if (hitThing is not Pawn && projectilePropertiesCE != null &&
                !projectilePropertiesCE.secondaryDamage.NullOrEmpty())
            {
                using var enumerator = projectilePropertiesCE.secondaryDamage.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    var secondaryDamage = enumerator.Current;
                    if (hitThing.Destroyed)
                    {
                        break;
                    }

                    var dinfo2 = new DamageInfo(secondaryDamage?.def, secondaryDamage.amount,
                        projectilePropertiesCE.GetArmorPenetration(1f), ExactRotation.eulerAngles.y, launcher, null,
                        def);
                    hitThing.TakeDamage(dinfo2).AssociateWithLog(logEntry_DamageResult);
                }
            }

            skipSound = true;
        }

        if (!skipSound)
        {
            SoundDefOf.BulletImpact_Ground.PlayOneShot(new TargetInfo(Position, map));
            if (castShadow)
            {
                FleckMaker.Static(ExactPosition, map, FleckDefOf.ShotHit_Dirt);
                var effect = projectileProperties?.effectGroundHit.Spawn();
                effect?.Trigger(this, this);
                if (Position.GetTerrain(map).takeSplashes)
                {
                    FleckMaker.WaterSplash(ExactPosition, map,
                        Mathf.Sqrt(def.projectile.GetDamageAmount(launcher)) * 1f, 4f);
                }
            }
        }

        // Either PENETRATION or RICOCHET or STOP can happen.
        var impactType = ImpactHelper.DetermineImpactType(this, hitThing, ref energyRemaining, isDeflectedByPawn);

        // DECREASE ENERGY
        if (impactType == ImpactType.PEN && energyRemaining > 0f)
        {
            if (projectileProperties.ammoType is AmmoType.AP or AmmoType.API or AmmoType.SABOT)
            {
                shotSpeed *= 0.9f;
                ImpactSpeedChanged();
            }
            else
            {
                shotSpeed *= 0.8f;
                ImpactSpeedChanged();
            }
        }

        // EITHER STOP or GROUND HIT
        landed = impactType == ImpactType.STOP || ticksToImpact <= 0 || energyRemaining <= 0f;

        ImpactBase(landed);
    }

    private void ImpactBase(bool destroyBullet = true)
    {
        var compExplosiveCE = this.TryGetComp<CompExplosiveCE>();
        if (compExplosiveCE != null && ExactPosition.ToIntVec3().IsValid)
        {
            compExplosiveCE.Explode(launcher, ExactPosition, Map);
        }

        if (Controller.settings.EnableAmmoSystem && compExplosiveCE == null && Position.IsValid &&
            def.projectile.preExplosionSpawnChance > 0f && def.projectile.preExplosionSpawnThingDef != null &&
            Rand.Value < def.projectile.preExplosionSpawnChance)
        {
            var preExplosionSpawnThingDef = def.projectile.preExplosionSpawnThingDef;
            if (preExplosionSpawnThingDef.IsFilth && Position.Walkable(Map))
            {
                FilthMaker.TryMakeFilth(Position, Map, preExplosionSpawnThingDef);
            }
            else if (Controller.settings.ReuseNeolithicProjectiles)
            {
                var thing = ThingMaker.MakeThing(preExplosionSpawnThingDef);
                thing.stackCount = 1;
                thing.SetForbidden(true, false);
                GenPlace.TryPlaceThing(thing, Position, Map, ThingPlaceMode.Near);
                LessonAutoActivator.TeachOpportunity(CE_ConceptDefOf.CE_ReusableNeolithicProjectiles, thing,
                    OpportunityType.GoodToKnow);
            }
        }

        if (def.projectile.explosionRadius > 0f && ExactPosition.y < 3f)
        {
            foreach (var thing2 in GenRadial.RadialDistinctThingsAround(ExactPosition.ToIntVec3(), Map,
                         3f + def.projectile.explosionRadius, true))
            {
                if (thing2 is Pawn pawn)
                {
                    ApplySuppression(pawn);
                }
            }
        }

        if (destroyBullet && !Destroyed)
        {
            Destroy();
        }
    }

    private bool CheckCellForCollision(IntVec3 cell)
    {
        var roofChecked = false;

        var mainThingList = new List<Thing>(Map.thingGrid.ThingsListAtFast(cell))
            .Where(t => t is Pawn || t.def.Fillage != FillCategory.None).ToList();

        //Find pawns in adjacent cells and append them to main list
        var adjList = new List<IntVec3>();
        adjList.AddRange(GenAdj
            .CellsAdjacentCardinal(cell, Rot4.FromAngleFlat(shotRotation), new IntVec2(collisionCheckSize, 0))
            .ToList());

        //Iterate through adjacent cells and find all the pawns
        foreach (var curCell in adjList)
        {
            if (curCell == cell || !curCell.InBounds(Map))
            {
                continue;
            }

            mainThingList.AddRange(Map.thingGrid.ThingsListAtFast(curCell)
                .Where(x => x is Pawn));

            if (Controller.settings.DebugDrawInterceptChecks)
            {
                Map.debugDrawer.FlashCell(curCell, 0.7f);
            }
        }

        //If the last position is above the wallCollisionHeight, we should check for roof intersections first
        if (LastPos.y > CollisionVertical.WallCollisionHeight)
        {
            if (TryCollideWithRoof(cell))
            {
                return true;
            }

            roofChecked = true;
        }

        foreach (var thing in mainThingList.Distinct().OrderBy(x => (x.DrawPos - LastPos).sqrMagnitude))
        {
            if ((thing == launcher || thing == mount) && !canTargetSelf)
            {
                continue;
            }

            // Check for collision
            if (thing == intendedTarget || def.projectile.alwaysFreeIntercept ||
                thing.Position.DistanceTo(OriginIV3) >= minCollisionDistance)
            {
                if (TryCollideWith(thing))
                {
                    return true;
                }
            }

            // Apply suppression. The height here is NOT that of the bullet in CELL,
            // it is the height at the END OF THE PATH. This is because SuppressionRadius
            // is not considered an EXACT limit.
            if (!(ExactPosition.y < SuppressionRadius))
            {
                continue;
            }

            if (thing is Pawn pawn)
            {
                ApplySuppression(pawn);
            }
        }

        //Finally check for intersecting with a roof (again).
        return !roofChecked && TryCollideWithRoof(cell);
    }

    private bool CheckForCollisionBetween()
    {
        var intVec = LastPos.ToIntVec3();
        var intVec2 = ExactPosition.ToIntVec3();
        if (!intVec.InBounds(Map) || !intVec2.InBounds(Map))
        {
            return false;
        }

        var baseType = GetType().BaseType;

        var methodInfo = baseType?.GetMethod("CheckIntercept", BindingFlags.NonPublic | BindingFlags.Instance);
        if (methodInfo != null)
        {
            var list = Map.listerThings.ThingsInGroup(ThingRequestGroup.ProjectileInterceptor);

            // ReSharper disable once ForCanBeConvertedToForeach
            for (var index = 0; index < list.Count; index++)
            {
                var thing = list[index];
                if (!(bool)methodInfo.Invoke(this,
                        new object[] { thing, thing.TryGetComp<CompProjectileInterceptor>(), false }))
                {
                    continue;
                }

                Destroy();
                return true;
            }
        }

        if (ticksToImpact < 0 || def.projectile.flyOverhead)
        {
            return false;
        }

        if (debugDrawIntercepts)
        {
            Map.debugDrawer.FlashLine(intVec, intVec2);
        }

        foreach (var intVec3 in from x in GenSight.PointsOnLineOfSight(intVec, intVec2).Union(new[]
                 {
                     intVec,
                     intVec2
                 }).Distinct()
                 orderby (x.ToVector3Shifted() - LastPos).MagnitudeHorizontalSquared()
                 select x)
        {
            if (CheckCellForCollision(intVec3))
            {
                return true;
            }

            if (debugDrawIntercepts)
            {
                Map.debugDrawer.FlashCell(intVec3, 1f, "o");
            }
        }

        return false;
    }

    public override void Tick()
    {
        // Lazy initialization
        projectileProperties ??= def.projectile as ProjectilePropertiesWithEffectsCE;

        // Try to bypass the ProjectileCE.Tick() which is overriden here.
        // Since it's first thing is to check if landed is true and just returns if it IS we try to trick it.
        var real_landed_val = landed;
        landed = true;
        base.Tick();
        landed = real_landed_val;

        // This is the same check mention above
        if (landed)
        {
            CombatEffectsCEMod.LogMessage($"{def.LabelCap} landed");
            if (!Destroyed)
            {
                Destroy();
            }

            return;
        }

        LastPos = ExactPosition;
        ticksToImpact--; // this increments our position along the trajectory!
        CombatEffectsCEMod.LogMessage($"{def.LabelCap} ticks to impact: {ticksToImpact}");

        if (!ExactPosition.InBounds(Map))
        {
            Position = LastPos.ToIntVec3();
            CombatEffectsCEMod.LogMessage($"{def.LabelCap} out of bounds");
            Destroy();
            return;
        }

        if (ticksToImpact >= 0 && !def.projectile.flyOverhead && CheckForCollisionBetween())
        {
            CombatEffectsCEMod.LogMessage($"{def.LabelCap} intercepted");
            return;
        }

        Position = ExactPosition.ToIntVec3();
        if (ticksToImpact == 60 && Find.TickManager.CurTimeSpeed == TimeSpeed.Normal)
        {
            def.projectile.soundImpactAnticipate?.PlayOneShot(this);
        }

        if (ticksToImpact <= 0)
        {
            CombatEffectsCEMod.LogMessage($"{def.LabelCap} should impact now");
            ImpactSomething();
            return;
        }

        ambientSustainer?.Maintain();
    }

    private bool TryCollideWith(Thing thing)
    {
        // if hit thing is it's own launcher and can't hit it self -> No hit!
        if (thing == launcher && !canTargetSelf || thing == lastThingHit)
        {
            return false;
        }

        // IntersectRay method of a Bound intersects the bounding box with the ray and gives us the distance to that point. If it returns false or null .. the ray missed the bound entirely
        if (!CE_Utility.GetBoundsFor(thing).IntersectRay(ShotLine, out var num))
        {
            return false;
        }

        // If distance square is greater than the distance square we traveled since last tick (this point and the last) -> No hit!
        if (num * num > ExactMinusLastPos.sqrMagnitude)
        {
            return false;
        }

        if (thing is Plant)
        {
            // Compute the chance that projectile hits the plant. Based on fillPercentage of plant and something.
            // this.OriginIV3 is the origin of the shot the gun it self.
            // I think [((thing.Position - this.OriginIV3).LengthHorizontal / 40f * this.AccuracyFactor] wanted a parentheses around (40f * this.AccuracyFactor) because now accuracy increases the chance
            var num2 = def.projectile.alwaysFreeIntercept
                ? 1f
                : (thing.Position - OriginIV3).LengthHorizontal / (40f * AccuracyFactor);
            var chance = thing.def.fillPercent * num2;
            if (Controller.settings.DebugShowTreeCollisionChance)
            {
                MoteMaker.ThrowText(thing.Position.ToVector3Shifted(), thing.Map, chance.ToString());
            }

            if (!Rand.Chance(chance))
            {
                return false;
            }
        }

        //GetPoint gives use the point the shotline ray hits the bounding box.
        var point = ShotLine.GetPoint(num);
        if (!point.InBounds(Map))
        {
            Log.Error(
                $"TryCollideWith out of bounds point from ShotLine: obj {thing.ThingID}, proj {ThingID}, dist {num}, point {point}");
        }

        ExactPosition = point;

        // We inject the penetration and ricochet logic around here.
        //this.landed = true; // This move to the Impact() function.
        if (debugDrawIntercepts)
        {
            MoteMaker.ThrowText(thing.Position.ToVector3Shifted(), thing.Map, "x", Color.red);
        }

        Impact(thing);
        return true;
    }

    private bool TryCollideWithRoof(IntVec3 cell)
    {
        if (!cell.Roofed(Map))
        {
            return false;
        }

        if (!CE_Utility.GetBoundsFor(cell, cell.GetRoof(Map)).IntersectRay(ShotLine, out var num))
        {
            return false;
        }

        if (num * num > ExactMinusLastPos.sqrMagnitude)
        {
            return false;
        }

        var point = ShotLine.GetPoint(num);

        ExactPosition = point;
        landed = true;
        if (debugDrawIntercepts)
        {
            MoteMaker.ThrowText(cell.ToVector3Shifted(), Map, "x", Color.red);
        }

        Impact(null);
        return true;
    }

    private new void ImpactSomething()
    {
        var intVec = ExactPosition.ToIntVec3();
        if (def.projectile.flyOverhead)
        {
            var roofDef = Map.roofGrid.RoofAt(intVec);
            if (roofDef != null)
            {
                if (roofDef.isThickRoof)
                {
                    def.projectile.soundHitThickRoof.PlayOneShot(new TargetInfo(intVec, Map));
                    Destroy();
                    return;
                }

                if (intVec.GetEdifice(Map) == null || intVec.GetEdifice(Map).def.Fillage != FillCategory.Full)
                {
                    RoofCollapserImmediate.DropRoofInCells(intVec, Map);
                }
            }
        }

        Thing firstPawn = intVec.GetFirstPawn(Map);
        if (firstPawn != null && TryCollideWith(firstPawn))
        {
            return;
        }

        var list = (from t in Map.thingGrid.ThingsListAt(intVec)
            where t is Pawn || t.def.Fillage > FillCategory.None
            select t).ToList();
        if (list.Count > 0)
        {
            // ReSharper disable once ForCanBeConvertedToForeach
            for (var index = 0; index < list.Count; index++)
            {
                var thing = list[index];
                if (TryCollideWith(thing))
                {
                    return;
                }
            }
        }

        ExactPosition = ExactPosition;
        landed = true;
        Impact(null);
    }

    /// -------------------------------------LAUNCH RELATED STUFF-------------------------------------------- ///
    public override void Launch(Thing launcher, Vector2 origin, float shotAngle, float shotRotation,
        float shotHeight = 0f, float shotSpeed = -1f, Thing equipment = null, float distance = -1f)
    {
        this.shotAngle = shotAngle;
        this.shotHeight = shotHeight;
        this.shotRotation = shotRotation;
        Launch(launcher, origin, equipment);
        if (shotSpeed > 0f)
        {
            this.shotSpeed = shotSpeed;
        }

        ticksToImpact = IntTicksToImpact;

        CombatEffectsCEMod.LogMessage(
            $"{def.LabelCap} launched, shotAngle: {shotAngle}, shotHeight: {shotHeight}, shotRotation: {shotRotation}, shotSpeed {shotSpeed}, ticksToImpact: {ticksToImpact}");
    }

    public override void Launch(Thing launcher, Vector2 origin, Thing equipment = null)
    {
        shotSpeed = def.projectile.speed;
        this.launcher = launcher;
        this.origin = origin;
        equipmentDef = equipment?.def;
        if (def.projectile.soundAmbient.NullOrUndefined())
        {
            return;
        }

        var info = SoundInfo.InMap(this, MaintenanceType.PerTick);
        ambientSustainer = def.projectile.soundAmbient.TrySpawnSustainer(info);
        CombatEffectsCEMod.LogMessage(
            $"{def.LabelCap} launched, equipmentDef: {equipmentDef}, launcher: {launcher}, origin: {origin}, shotSpeed {shotSpeed}");
    }

    private void ImpactSpeedChanged()
    {
        origin = new Vector2(impactPosition.x, impactPosition.z);
        shotHeight = Height;
        ticksToImpact = 0;
        intTicksToImpact = -1;
        startingTicksToImpactInt = -1;
        lastHeightTick = -1;
        destinationInt = new Vector3(0f, 0f, -1f);
        Vec2Position();
        // Note : For some reason if I use IntTicksToImpact after an impact the new intTicks
        ticksToImpact = IntTicksToImpact;
        CombatEffectsCEMod.LogMessage(
            $"{def.LabelCap} ImpactSpeedChanged, origin: {origin}, shotHeight: {shotHeight}, ticksToImpact: {ticksToImpact}");
    }

    private Vector2 Vec2Position(float ticks = -1f)
    {
        if (ticks < 0f)
        {
            ticks = fTicks;
        }

        return ticks != StartingTicksToImpact
            ? Vector2.Lerp(origin, Destination, ticks / StartingTicksToImpact)
            : Vector2.Lerp(origin, Destination, 1f);
    }

    private float GetFlightTime()
    {
        return ((Mathf.Sin(shotAngle) * shotSpeed) +
                Mathf.Sqrt(Mathf.Pow(Mathf.Sin(shotAngle) * shotSpeed, 2f) + (2f * GravityFactor * shotHeight))) /
               GravityFactor;
    }

    private float GetHeightAtTicks(int ticks)
    {
        var num = ticks / 60f;
        return (float)Math.Round(
            shotHeight + (shotSpeed * Mathf.Sin(shotAngle) * num) - (GravityFactor * num * num / 2f), 3);
    }
}