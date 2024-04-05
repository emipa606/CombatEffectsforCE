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

[StaticConstructorOnStartup]
public class BulletCESparky : ProjectileCE
{
    static BulletCESparky()
    {
        BlockerRegistry.RegisterBeforeCollideWithCallback(beforeCollideWithCallback);
    }
    private static bool beforeCollideWithCallback(ProjectileCE projectile, Thing hitThing)
    {
        if (projectile is BulletCESparky sparky)
        {
            if (sparky.lastThingHit == hitThing)
            {
                return true;
            }
        }
        return false;
    }
    private float energyRemaining = 100f;
    private Vector3 impactPosition;

    private Thing lastThingHit;
    public ProjectilePropertiesWithEffectsCE projectileProperties;

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


}