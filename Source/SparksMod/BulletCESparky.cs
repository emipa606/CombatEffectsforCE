using System;
using System.Collections.Generic;
using System.Linq;
using CombatEffectsCE;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace CombatExtended
{


    // Token: 0x02000070 RID: 112
    public class BulletCESparky : ProjectileCE
    {
        private void ApplySuppression(Pawn pawn)
        {
            ShieldBelt shieldBelt = null;
            if (pawn.RaceProps.Humanlike)
            {
                List<Apparel> wornApparel = pawn.apparel.WornApparel;
                for (var i = 0; i < wornApparel.Count; i++)
                {
                    if (wornApparel[i] is ShieldBelt shieldBelt2)
                    {
                        shieldBelt = shieldBelt2;
                        break;
                    }
                }
            }
            CompSuppressable compSuppressable = pawn.TryGetComp<CompSuppressable>();
            if (compSuppressable != null)
            {
                Faction faction = pawn.Faction;
                Thing thing = launcher;
                if (faction != (thing?.Faction) && (shieldBelt == null || (shieldBelt != null && shieldBelt.ShieldState == ShieldState.Resetting)))
                {
                    suppressionAmount = def.projectile.GetDamageAmount(1f, null);
                    var num = (!(def.projectile is ProjectilePropertiesCE projectilePropertiesCE)) ? 0f : projectilePropertiesCE.GetArmorPenetration(1f, null);
                    suppressionAmount *= 1f - Mathf.Clamp(pawn.GetStatValue(CE_StatDefOf.AverageSharpArmor, true) * 0.5f / num, 0f, 1f);
                    compSuppressable.AddSuppression(suppressionAmount, OriginIV3);
                }
            }
        }

        // Token: 0x06000259 RID: 601 RVA: 0x00014A1B File Offset: 0x00012C1B
        private void LogImpact(Thing hitThing, out LogEntry_DamageResult logEntry)
        {
            logEntry = new BattleLogEntry_RangedImpact(launcher, hitThing, intendedTarget, equipmentDef, def, null);
            if (!(launcher is AmmoThing))
            {
                Find.BattleLog.Add(logEntry);
            }
        }

        // Token: 0x0600025A RID: 602 RVA: 0x00014A58 File Offset: 0x00012C58
        protected override void Impact(Thing hitThing)
        {
            // Okay this is not entirely good
            // I removed the timer since it's not really needed.
            // But when ticksToImpact is zero it can still hit the target a second time!
            if (hitThing == lastThingHit && ticksToImpact > 0 && impactPosition.y > 0)
            {
                //After penetration the bullet can hit the same target multiple times. This tries to prevent that.
                return;
            }
            else
            {
                lastThingHit = hitThing;
            }


            var flag = launcher is AmmoThing;
            Map map = Map;
            LogEntry_DamageResult logEntry_DamageResult = null;

            var isDeflectedByPawn = false;

            if ((logMisses || (!logMisses && hitThing != null && (hitThing is Pawn || hitThing is Building_Turret))) && !flag)
            {
                LogImpact(hitThing, out logEntry_DamageResult);
            }
            if (hitThing != null)
            {
                var angleY = ExactRotation.eulerAngles.y; // This is the bullet heading angle in respect to the north vector around the up vector

                if (hitThing is Building)
                {
                    var shotAngle = angleY - 180f;

                    if (hitThing.def.MadeFromStuff == true)
                    {
                        if (hitThing.Stuff.label == "wood" && projectileProperties.effectWoodWallHit != null) // Wooden wall
                        {
                            projectileProperties.effectWoodWallHit.children[0].angle = new FloatRange(shotAngle - 90f, shotAngle + 90f);
                            Effecter eff = projectileProperties.effectWoodWallHit.Spawn();
                            eff.Trigger(hitThing, hitThing);
                        }
                        else if (projectileProperties.effectWoodWallHit != null) // Non-Wood Wall
                        {
                            projectileProperties.effectStoneWallHit.children[1].angle = new FloatRange(shotAngle - 90f, shotAngle + 90f);
                            Effecter eff = projectileProperties.effectStoneWallHit.Spawn();
                            eff.Trigger(hitThing, hitThing);
                        }
                    }
                    else if (projectileProperties.effectStoneWallHit != null) //Natural Stone
                    {
                        projectileProperties.effectStoneWallHit.children[1].angle = new FloatRange(shotAngle - 90f, shotAngle + 90f);
                        Effecter eff = projectileProperties.effectStoneWallHit.Spawn();
                        eff.Trigger(hitThing, hitThing);
                    }


                    if (hitThing.def.MadeFromStuff == true)
                    {
                        Color hitThingColor = hitThing.Stuff.graphic.color;
                        hitThingColor.a = 0.6f;
                        projectileProperties.effectBuildingBits.children[0].moteDef.graphicData.color = hitThingColor;
                    }
                    projectileProperties.effectBuildingBits.children[0].angle = new FloatRange(shotAngle - 45f, shotAngle + 45f);
                    Effecter effecter = projectileProperties.effectBuildingBits.Spawn();
                    effecter.Trigger(hitThing, hitThing);

                    effecter = projectileProperties.effectPuff.Spawn();
                    effecter.Trigger(this, hitThing);

                }
                var damageAmount = def.projectile.GetDamageAmount(1f, null);
                DamageDefExtensionCE damageDefExtensionCE = def.projectile.damageDef.GetModExtension<DamageDefExtensionCE>() ?? new DamageDefExtensionCE();
                var projectilePropertiesCE = (ProjectilePropertiesCE)def.projectile;
                var flag2 = this.def.projectile.damageDef.armorCategory == DamageArmorCategoryDefOf.Sharp;
                var armorPenetration = (!flag2) ? projectilePropertiesCE.armorPenetrationBlunt : projectilePropertiesCE.armorPenetrationSharp;
                var dinfo = new DamageInfo(def.projectile.damageDef, damageAmount, armorPenetration, ExactRotation.eulerAngles.y, launcher, null, def, DamageInfo.SourceCategory.ThingOrUnknown, null);
                BodyPartDepth depth = (damageDefExtensionCE != null && damageDefExtensionCE.harmOnlyOutsideLayers) ? BodyPartDepth.Outside : BodyPartDepth.Undefined;
                BodyPartHeight collisionBodyHeight = new CollisionVertical(hitThing).GetCollisionBodyHeight(ExactPosition.y);
                dinfo.SetBodyRegion(collisionBodyHeight, depth);
                if (damageDefExtensionCE != null && damageDefExtensionCE.harmOnlyOutsideLayers)
                {
                    dinfo.SetBodyRegion(BodyPartHeight.Undefined, BodyPartDepth.Outside);
                }
                if (flag && hitThing is Pawn pawn)
                {
                    logEntry_DamageResult = new BattleLogEntry_DamageTaken(pawn, DefDatabase<RulePackDef>.GetNamed("DamageEvent_CookOff", true), null);
                    Find.BattleLog.Add(logEntry_DamageResult);
                }

                DamageWorker.DamageResult dmgRes = hitThing.TakeDamage(dinfo);
                //Log.Message($"is it deflected? : {dmgRes.deflected}");
                if (CombatEffectsCEMod.instance.Settings.ExtraBlood && !dmgRes.deflected && hitThing is Pawn && projectileProperties.effectBloodHit != null)
                {
                    //Log.Message("Hit someone!");
                    //string m = $"It's meat color : {hitThing.def.race.meatColor.ToString()}";
                    //Log.Message("Show extra blood");                       
                    if (hitThing.def.race.IsMechanoid)
                    {
                        var bloodColor = new Color(0.04f, 0.04f, 0.04f, 0.7f);
                        projectileProperties.effectBloodHit.children[0].moteDef.graphicData.color = bloodColor;
                        ((MyGraphicData)((MotePropertiesFilthy)projectileProperties.effectBloodHit.children[0].moteDef.mote).filthTrace.graphicData).ChangeGraphicColor(bloodColor);
                    }
                    else
                    {
                        Color bloodColor = hitThing.def.race.meatColor;
                        if (bloodColor == Color.white)
                        {
                            //Log.Message("Blood color changing!");
                            bloodColor = new Color(0.5f, 0.1f, 0.1f);
                        }
                        //bloodColor.a = 1f;
                        //m = $"Requested color : {bloodColor.ToString()}";
                        //Log.Message(m);
                        //((SparksMod.MyGraphicData)this.projectileProperties.effectBloodHit.children[0].moteDef.graphicData).changeGraphicColor(bloodColor);
                        //((SparksMod.MyGraphicData)((SparksMod.MotePropertiesFilthy)this.projectileProperties.effectBloodHit.children[0].moteDef.mote).filthTrace.graphicData).changeGraphicColor(bloodColor);

                        projectileProperties.effectBloodHit.children[0].moteDef.graphicData.color = bloodColor;
                        ((MyGraphicData)((MotePropertiesFilthy)projectileProperties.effectBloodHit.children[0].moteDef.mote).filthTrace.graphicData).ChangeGraphicColor(bloodColor);
                    }
                    Effecter bloodEffect = ((ProjectilePropertiesWithEffectsCE)def.projectile).effectBloodHit.Spawn();
                    bloodEffect.Trigger(hitThing, hitThing);
                }
                isDeflectedByPawn = dmgRes.deflected;

                //Log the result of the hit!
                dmgRes.AssociateWithLog(logEntry_DamageResult);
                if (hitThing is Pawn || projectilePropertiesCE == null || projectilePropertiesCE.secondaryDamage.NullOrEmpty())
                {
                    goto IL_2A6;
                }
                using (List<SecondaryDamage>.Enumerator enumerator = projectilePropertiesCE.secondaryDamage.GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        SecondaryDamage secondaryDamage = enumerator.Current;
                        if (hitThing.Destroyed)
                        {
                            break;
                        }
                        var dinfo2 = new DamageInfo(secondaryDamage.def, secondaryDamage.amount, projectilePropertiesCE.GetArmorPenetration(1f, null), ExactRotation.eulerAngles.y, launcher, null, def, DamageInfo.SourceCategory.ThingOrUnknown, null);
                        hitThing.TakeDamage(dinfo2).AssociateWithLog(logEntry_DamageResult);
                    }
                    goto IL_2A6;
                }
            }

            SoundDefOf.BulletImpact_Ground.PlayOneShot(new TargetInfo(Position, map, false));
            if (castShadow)
            {
                MoteMaker.MakeStaticMote(ExactPosition, map, ThingDefOf.Mote_ShotHit_Dirt, 1f);
                Effecter effect = projectileProperties.effectGroundHit.Spawn();
                effect.Trigger(this, this);
                if (Position.GetTerrain(map).takeSplashes)
                {
                    MoteMaker.MakeWaterSplash(ExactPosition, map, Mathf.Sqrt(def.projectile.GetDamageAmount(launcher, null)) * 1f, 4f);
                }
            }
        IL_2A6:
            // Either PENETRATION or RICOCHET or STOP can happen.
            ImpactType impactType = CombatEffectsCE.ImpactHelper.DetermineImpactType(this, hitThing, ref energyRemaining, isDeflectedByPawn);

            // DECREASE ENERGY
            if (impactType == CombatEffectsCE.ImpactType.PEN && energyRemaining > 0f)
            {
                if (projectileProperties.ammoType == CombatEffectsCE.AmmoType.AP
                    || projectileProperties.ammoType == CombatEffectsCE.AmmoType.API
                    || projectileProperties.ammoType == CombatEffectsCE.AmmoType.SABOT)
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
            landed = impactType == CombatEffectsCE.ImpactType.STOP || (ticksToImpact <= 0) || energyRemaining <= 0f;

            ImpactBase(hitThing, landed);
        }

        private void ImpactBase(Thing hitThing, bool destroyBullet = true)
        {
            CompExplosiveCE compExplosiveCE = this.TryGetComp<CompExplosiveCE>();
            if (compExplosiveCE != null && ExactPosition.ToIntVec3().IsValid)
            {
                compExplosiveCE.Explode(launcher, ExactPosition, Map, 1f);
            }
            if (Controller.settings.EnableAmmoSystem && compExplosiveCE == null && Position.IsValid && def.projectile.preExplosionSpawnChance > 0f && def.projectile.preExplosionSpawnThingDef != null && Rand.Value < def.projectile.preExplosionSpawnChance)
            {
                ThingDef preExplosionSpawnThingDef = def.projectile.preExplosionSpawnThingDef;
                if (preExplosionSpawnThingDef.IsFilth && Position.Walkable(Map))
                {
                    FilthMaker.TryMakeFilth(Position, Map, preExplosionSpawnThingDef, 1);
                }
                else if (Controller.settings.ReuseNeolithicProjectiles)
                {
                    Thing thing = ThingMaker.MakeThing(preExplosionSpawnThingDef, null);
                    thing.stackCount = 1;
                    thing.SetForbidden(true, false);
                    GenPlace.TryPlaceThing(thing, Position, Map, ThingPlaceMode.Near, null, null);
                    LessonAutoActivator.TeachOpportunity(CE_ConceptDefOf.CE_ReusableNeolithicProjectiles, thing, OpportunityType.GoodToKnow);
                }
            }
            if (def.projectile.explosionRadius > 0f && ExactPosition.y < 3f)
            {
                foreach (Thing thing2 in GenRadial.RadialDistinctThingsAround(ExactPosition.ToIntVec3(), Map, 3f + def.projectile.explosionRadius, true))
                {
                    if (thing2 is Pawn pawn)
                    {
                        ApplySuppression(pawn);
                    }
                }
            }

            if (destroyBullet && !Destroyed)
            {
                Destroy(DestroyMode.Vanish);
            }
        }

        private bool CheckCellForCollision(IntVec3 cell)
        {
            var flag = false;
            var justWallsRoofs = false;
            float num = (cell - OriginIV3).LengthHorizontalSquared;
            if ((!def.projectile.alwaysFreeIntercept && minCollisionSqr <= 1f) ? (num < 1f) : (num < Mathf.Min(144f, minCollisionSqr / 4f)))
            {
                justWallsRoofs = true;
            }
            var list = new List<Thing>(Map.thingGrid.ThingsListAtFast(cell)).Where(delegate (Thing t)
            {
                if (!justWallsRoofs)
                {
                    return t is Pawn || t.def.Fillage > FillCategory.None;
                }
                return t.def.Fillage == FillCategory.Full;
            }).ToList();
            if (!justWallsRoofs)
            {
                var list2 = new List<IntVec3>();
                list2.AddRange(GenAdj.CellsAdjacentCardinal(cell, Rot4.FromAngleFlat(shotRotation), new IntVec2(5, 0)).ToList());
                foreach (IntVec3 intVec in list2)
                {
                    if (intVec != cell && intVec.InBounds(Map))
                    {
                        list.AddRange(from x in Map.thingGrid.ThingsListAtFast(intVec)
                                      where x is Pawn
                                      select x);
                        if (debugDrawIntercepts)
                        {
                            Map.debugDrawer.FlashCell(intVec, 0.7f, null, 50);
                        }
                    }
                }
            }
            if (LastPos.y > 2f)
            {
                if (TryCollideWithRoof(cell))
                {
                    return true;
                }
                flag = true;
            }
            foreach (Thing thing in from x in list.Distinct() orderby (x.DrawPos - LastPos).sqrMagnitude select x)
            {
                // Modify : Added check to check if we hit this thing already than keep searching. Probably I will end up removing the similar check from Impact()
                if ((thing != launcher && thing != mount) || canTargetSelf)
                {
                    if (TryCollideWith(thing))
                    {
                        return true;
                    }
                    if (!justWallsRoofs && ExactPosition.y < 3f)
                    {
                        if (thing is Pawn pawn)
                        {
                            ApplySuppression(pawn);
                        }
                    }
                }
            }
            return !flag && TryCollideWithRoof(cell);
        }

        private bool CheckForCollisionBetween()
        {
            var intVec = LastPos.ToIntVec3();
            var intVec2 = ExactPosition.ToIntVec3();
            if (!intVec.InBounds(Map) || !intVec2.InBounds(Map))
            {
                return false;
            }
            if (debugDrawIntercepts)
            {
                Map.debugDrawer.FlashLine(intVec, intVec2, 50, SimpleColor.White);
            }
            foreach (IntVec3 intVec3 in from x in GenSight.PointsOnLineOfSight(intVec, intVec2).Union(new IntVec3[]
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
                    Map.debugDrawer.FlashCell(intVec3, 1f, "o", 50);
                }
            }
            return false;
        }

        public override void Tick()
        {
            // Lazy initialization
            if (projectileProperties == null)
            {
                projectileProperties = def.projectile as ProjectilePropertiesWithEffectsCE;
            }

            // Try to bypass the ProjectileCE.Tick() which is overriden here.
            // Since it's first thing is to check if landed is true and just returns if it IS we try to trick it.
            var real_landed_val = landed;
            landed = true;
            base.Tick();
            landed = real_landed_val;

            // This is the same check mention above
            if (landed)
            {
                if (!Destroyed)
                {
                    Destroy(DestroyMode.Vanish);
                }
                return;
            }
            LastPos = ExactPosition;
            ticksToImpact--; // this increments our position along the trajectory!
            if (!ExactPosition.InBounds(Map))
            {
                Position = LastPos.ToIntVec3();
                Destroy(DestroyMode.Vanish);
                return;
            }
            if (ticksToImpact >= 0 && !def.projectile.flyOverhead && CheckForCollisionBetween())
            {
                return;
            }
            Position = ExactPosition.ToIntVec3();
            if (ticksToImpact == 60 && Find.TickManager.CurTimeSpeed == TimeSpeed.Normal && def.projectile.soundImpactAnticipate != null)
            {
                def.projectile.soundImpactAnticipate.PlayOneShot(this);
            }
            if (ticksToImpact <= 0)
            {
                ImpactSomething();
                return;
            }
            if (ambientSustainer != null)
            {
                ambientSustainer.Maintain();
            }
        }

        private bool TryCollideWith(Thing thing)
        {
            // if hit thing is it's own launcher and can't hit it self -> No hit!
            if ((thing == launcher && !canTargetSelf) || thing == lastThingHit)
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
                var num2 = def.projectile.alwaysFreeIntercept ? 1f : ((thing.Position - OriginIV3).LengthHorizontal / (40f * AccuracyFactor));
                var chance = thing.def.fillPercent * num2;
                if (Controller.settings.DebugShowTreeCollisionChance)
                {
                    MoteMaker.ThrowText(thing.Position.ToVector3Shifted(), thing.Map, chance.ToString(), -1f);
                }
                if (!Rand.Chance(chance))
                {
                    return false;
                }
            }
            //GetPoint gives use the point the shotline ray hits the bounding box.
            Vector3 point = ShotLine.GetPoint(num);
            if (!point.InBounds(Map))
            {
                Log.Error(string.Concat(new object[]
                {
                "TryCollideWith out of bounds point from ShotLine: obj ",
                thing.ThingID,
                ", proj ",
                ThingID,
                ", dist ",
                num,
                ", point ",
                point
                    }), false);
            }
            ExactPosition = point;

            // We inject the penetration and ricochet logic around here.
            //this.landed = true; // This move to the Impact() function.
            if (debugDrawIntercepts)
            {
                MoteMaker.ThrowText(thing.Position.ToVector3Shifted(), thing.Map, "x", Color.red, -1f);
            }

            //if (!(thing is Pawn))
            //{
            //    Log.Message($"{CombatEffectsCE.ImpactHelper.determineMaterial(thing)}");
            //}

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
            Vector3 point = ShotLine.GetPoint(num);

            ExactPosition = point;
            landed = true;
            if (debugDrawIntercepts)
            {
                MoteMaker.ThrowText(cell.ToVector3Shifted(), Map, "x", Color.red, -1f);
            }
            Impact(null);
            return true;
        }

        private void ImpactSomething()
        {
            var intVec = ExactPosition.ToIntVec3();
            if (def.projectile.flyOverhead)
            {
                RoofDef roofDef = Map.roofGrid.RoofAt(intVec);
                if (roofDef != null)
                {
                    if (roofDef.isThickRoof)
                    {
                        def.projectile.soundHitThickRoof.PlayOneShot(new TargetInfo(intVec, Map, false));
                        Destroy(DestroyMode.Vanish);
                        return;
                    }
                    if (intVec.GetEdifice(Map) == null || intVec.GetEdifice(Map).def.Fillage != FillCategory.Full)
                    {
                        RoofCollapserImmediate.DropRoofInCells(intVec, Map, null);
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
                foreach (Thing thing in list)
                {
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

        public override void Launch(Thing launcher, Vector2 origin, float shotAngle, float shotRotation, float shotHeight = 0f, float shotSpeed = -1f, Thing equipment = null)
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
        }

        // Token: 0x060002A2 RID: 674 RVA: 0x0001646C File Offset: 0x0001466C
        public override void Launch(Thing launcher, Vector2 origin, Thing equipment = null)
        {
            shotSpeed = def.projectile.speed;
            this.launcher = launcher;
            this.origin = origin;
            equipmentDef = (equipment?.def);
            if (!def.projectile.soundAmbient.NullOrUndefined())
            {
                var info = SoundInfo.InMap(this, MaintenanceType.PerTick);
                ambientSustainer = def.projectile.soundAmbient.TrySpawnSustainer(info);
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
            Vec2Position(-1f);
            // Note : For some reason if I use IntTicksToImpact after an impact the new intTicks
            ticksToImpact = IntTicksToImpact;
        }

        public override Vector3 ExactPosition
        {
            get
            {
                if (landed)
                {
                    return impactPosition;
                }
                Vector2 vector = Vec2Position(-1f);
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
                Vector2 drawPosV = DrawPosV2;
                return new Vector3(drawPosV.x, def.Altitude, drawPosV.y);
            }
        }

        public new Vector2 DrawPosV2 => Vec2Position(-1f) + new Vector2(0f, Height - (shotHeight * ((StartingTicksToImpact - fTicks) / StartingTicksToImpact)));

        private Vector2 Vec2Position(float ticks = -1f)
        {
            if (ticks < 0f)
            {
                ticks = fTicks;
            }
            if (ticks != StartingTicksToImpact)
            {
                return Vector2.Lerp(origin, Destination, ticks / StartingTicksToImpact);
            }
            else
            {
                // I had to treat this special case because of 0/0 = NaN problems
                return Vector2.Lerp(origin, Destination, 1f);
            }
        }

        protected new int FlightTicks => IntTicksToImpact - ticksToImpact;

        protected new float fTicks
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

        protected new int IntTicksToImpact
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

        protected new float StartingTicksToImpact
        {
            get
            {
                if (startingTicksToImpactInt < 0f)
                {
                    if (shotHeight < 0.001f)
                    {
                        if (shotAngle < 0f)
                        {
                            destinationInt = origin;
                            startingTicksToImpactInt = 0f;
                            ImpactSomething();
                            return 0f;
                        }
                        startingTicksToImpactInt = (origin - Destination).magnitude / (Mathf.Cos(shotAngle) * shotSpeed) * 60f;
                        return startingTicksToImpactInt;
                    }
                    else
                    {
                        startingTicksToImpactInt = GetFlightTime() * 60f;
                    }
                }
                return startingTicksToImpactInt;
            }
        }

        private float GetFlightTime()
        {
            return ((Mathf.Sin(shotAngle) * shotSpeed) + Mathf.Sqrt(Mathf.Pow(Mathf.Sin(shotAngle) * shotSpeed, 2f) + (2f * GravityFactor * shotHeight))) / GravityFactor;
        }

        private float GetHeightAtTicks(int ticks)
        {
            var num = ticks / 60f;
            return (float)Math.Round(shotHeight + (shotSpeed * Mathf.Sin(shotAngle) * num) - (GravityFactor * num * num / 2f), 3);
        }

        public new float Height
        {
            get
            {
                if (lastHeightTick != FlightTicks)
                {
                    heightInt = (ticksToImpact > 0) ? GetHeightAtTicks(FlightTicks) : 0f;
                    lastHeightTick = FlightTicks;
                }
                return heightInt;
            }
        }

        private float GravityFactor
        {
            get
            {
                if (_gravityFactor < 0f)
                {
                    _gravityFactor = 9.8f;
                    if (def.projectile is ProjectilePropertiesCE projectilePropertiesCE)
                    {
                        _gravityFactor = projectilePropertiesCE.Gravity;
                    }
                }
                return _gravityFactor;
            }
        }

        private Vector3 LastPos
        {
            get
            {
                if (lastExactPos.x < -999f)
                {
                    Vector2 vector = Vec2Position(FlightTicks - 1);
                    lastExactPos = new Vector3(vector.x, GetHeightAtTicks(FlightTicks - 1), vector.y);
                }
                return lastExactPos;
            }
            set => lastExactPos = value;
        }


        // Token: 0x040001BE RID: 446
        private const float StunChance = 0.1f;
        public ProjectilePropertiesWithEffectsCE projectileProperties = null;

        public Thing lastThingHit;
        public float id = Rand.Value;
        public bool debugDrawIntercepts = false;

        private float startingTicksToImpactInt = -1f;
        private int intTicksToImpact = -1;
        private Ray shotLine;
        private readonly int lastShotLine = -1;
        private Vector3 lastExactPos = new Vector3(-1000f, 0f, 0f);
        private float _gravityFactor = -1f;
        private Sustainer ambientSustainer;
        private float suppressionAmount;
        private float energyRemaining = 100f;
        private Vector3 impactPosition;
        private IntVec3 originInt = new IntVec3(0, -1000, 0);
        private int lastHeightTick = -1;
        private float heightInt;
        private static readonly List<IntVec3> checkedCells = new List<IntVec3>();


    }
}
