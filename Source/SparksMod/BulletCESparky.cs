using System;
using System.Collections.Generic;
using System.Linq;
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
                for (int i = 0; i < wornApparel.Count; i++)
                {
                    ShieldBelt shieldBelt2 = wornApparel[i] as ShieldBelt;
                    if (shieldBelt2 != null)
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
                Thing thing = this.launcher;
                if (faction != ((thing != null) ? thing.Faction : null) && (shieldBelt == null || (shieldBelt != null && shieldBelt.ShieldState == ShieldState.Resetting)))
                {
                    this.suppressionAmount = (float)this.def.projectile.GetDamageAmount(1f, null);
                    ProjectilePropertiesCE projectilePropertiesCE = this.def.projectile as ProjectilePropertiesCE;
                    float num = (projectilePropertiesCE == null) ? 0f : projectilePropertiesCE.GetArmorPenetration(1f, null);
                    this.suppressionAmount *= 1f - Mathf.Clamp(pawn.GetStatValue(CE_StatDefOf.AverageSharpArmor, true) * 0.5f / num, 0f, 1f);
                    compSuppressable.AddSuppression(this.suppressionAmount, this.OriginIV3);
                }
            }
        }

        // Token: 0x06000259 RID: 601 RVA: 0x00014A1B File Offset: 0x00012C1B
        private void LogImpact(Thing hitThing, out LogEntry_DamageResult logEntry)
        {
            logEntry = new BattleLogEntry_RangedImpact(this.launcher, hitThing, this.intendedTarget, this.equipmentDef, this.def, null);
            if (!(this.launcher is AmmoThing))
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


            bool flag = this.launcher is AmmoThing;
            Map map = base.Map;
            LogEntry_DamageResult logEntry_DamageResult = null;
            
            bool isDeflectedByPawn = false;

            if ((this.logMisses || (!this.logMisses && hitThing != null && (hitThing is Pawn || hitThing is Building_Turret))) && !flag)
            {
                this.LogImpact(hitThing, out logEntry_DamageResult);
            }
            if (hitThing != null)
            {
                float angleY = this.ExactRotation.eulerAngles.y; // This is the bullet heading angle in respect to the north vector around the up vector

                if (hitThing is Building)
                {                 
                    float shotAngle = angleY - 180f;

                    if (hitThing.def.MadeFromStuff == true)
                    {
                        if (hitThing.Stuff.label == "wood" && this.projectileProperties.effectWoodWallHit != null) // Wooden wall
                        {
                            this.projectileProperties.effectWoodWallHit.children[0].angle = new FloatRange(shotAngle - 90f, shotAngle + 90f);
                            Effecter eff = this.projectileProperties.effectWoodWallHit.Spawn();
                            eff.Trigger(hitThing, hitThing);
                        }
                        else if (this.projectileProperties.effectWoodWallHit != null) // Non-Wood Wall
                        {
                            this.projectileProperties.effectStoneWallHit.children[1].angle = new FloatRange(shotAngle - 90f, shotAngle + 90f);
                            Effecter eff = this.projectileProperties.effectStoneWallHit.Spawn();
                            eff.Trigger(hitThing, hitThing);
                        }
                    }
                    else if (this.projectileProperties.effectStoneWallHit != null) //Natural Stone
                    {
                        this.projectileProperties.effectStoneWallHit.children[1].angle = new FloatRange(shotAngle - 90f, shotAngle + 90f);
                        Effecter eff = this.projectileProperties.effectStoneWallHit.Spawn();
                        eff.Trigger(hitThing, hitThing);
                    }


                    if (hitThing.def.MadeFromStuff == true)
                    {
                        Color hitThingColor = hitThing.Stuff.graphic.color;
                        hitThingColor.a = 0.6f;
                        this.projectileProperties.effectBuildingBits.children[0].moteDef.graphicData.color = hitThingColor;
                    }                    
                    this.projectileProperties.effectBuildingBits.children[0].angle = new FloatRange(shotAngle - 45f, shotAngle + 45f);
                    Effecter effecter = this.projectileProperties.effectBuildingBits.Spawn();
                    effecter.Trigger(hitThing, hitThing);

                    effecter = this.projectileProperties.effectPuff.Spawn();
                    effecter.Trigger(this, hitThing);

                }
                int damageAmount = this.def.projectile.GetDamageAmount(1f, null);
                DamageDefExtensionCE damageDefExtensionCE = this.def.projectile.damageDef.GetModExtension<DamageDefExtensionCE>() ?? new DamageDefExtensionCE();
                ProjectilePropertiesCE projectilePropertiesCE = (ProjectilePropertiesCE)this.def.projectile;
                DamageInfo dinfo = new DamageInfo(this.def.projectile.damageDef, (float)damageAmount, projectilePropertiesCE.GetArmorPenetration(1f, null), this.ExactRotation.eulerAngles.y, this.launcher, null, this.def, DamageInfo.SourceCategory.ThingOrUnknown, null);
                BodyPartDepth depth = (damageDefExtensionCE != null && damageDefExtensionCE.harmOnlyOutsideLayers) ? BodyPartDepth.Outside : BodyPartDepth.Undefined;
                BodyPartHeight collisionBodyHeight = new CollisionVertical(hitThing).GetCollisionBodyHeight(this.ExactPosition.y);
                dinfo.SetBodyRegion(collisionBodyHeight, depth);
                if (damageDefExtensionCE != null && damageDefExtensionCE.harmOnlyOutsideLayers)
                {
                    dinfo.SetBodyRegion(BodyPartHeight.Undefined, BodyPartDepth.Outside);
                }
                if (flag && hitThing is Pawn)
                {
                    logEntry_DamageResult = new BattleLogEntry_DamageTaken((Pawn)hitThing, DefDatabase<RulePackDef>.GetNamed("DamageEvent_CookOff", true), null);
                    Find.BattleLog.Add(logEntry_DamageResult);
                }

                DamageWorker.DamageResult dmgRes = hitThing.TakeDamage(dinfo);
                //Log.Message($"is it deflected? : {dmgRes.deflected}");
                if (!dmgRes.deflected)
                {
                    if (hitThing is Pawn && this.projectileProperties.effectBloodHit != null)
                    {
                        //Log.Message("Hit someone!");
                        //string m = $"It's meat color : {hitThing.def.race.meatColor.ToString()}";
                        //Log.Message(m);                        
                        if (hitThing.def.race.IsMechanoid)
                        {
                            Color bloodColor = new Color(0.04f, 0.04f, 0.04f, 0.7f);
                            this.projectileProperties.effectBloodHit.children[0].moteDef.graphicData.color = bloodColor;
                            ((CombatEffectsCE.MyGraphicData)((CombatEffectsCE.MotePropertiesFilthy)this.projectileProperties.effectBloodHit.children[0].moteDef.mote).filthTrace.graphicData).changeGraphicColor(bloodColor);
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

                            this.projectileProperties.effectBloodHit.children[0].moteDef.graphicData.color = bloodColor;
                            ((CombatEffectsCE.MyGraphicData)((CombatEffectsCE.MotePropertiesFilthy)this.projectileProperties.effectBloodHit.children[0].moteDef.mote).filthTrace.graphicData).changeGraphicColor(bloodColor);
                        }
                        Effecter bloodEffect = ((ProjectilePropertiesWithEffectsCE)this.def.projectile).effectBloodHit.Spawn();
                        bloodEffect.Trigger(hitThing, hitThing);
                    }
                }
                isDeflectedByPawn = dmgRes.deflected;

                //Log the result of the hit!
                dmgRes.AssociateWithLog(logEntry_DamageResult);
                if (hitThing is Pawn || projectilePropertiesCE == null || projectilePropertiesCE.secondaryDamage.NullOrEmpty<SecondaryDamage>())
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
                        DamageInfo dinfo2 = new DamageInfo(secondaryDamage.def, (float)secondaryDamage.amount, projectilePropertiesCE.GetArmorPenetration(1f, null), this.ExactRotation.eulerAngles.y, this.launcher, null, this.def, DamageInfo.SourceCategory.ThingOrUnknown, null);
                        hitThing.TakeDamage(dinfo2).AssociateWithLog(logEntry_DamageResult);
                    }
                    goto IL_2A6;
                }
            }
            
            SoundDefOf.BulletImpact_Ground.PlayOneShot(new TargetInfo(base.Position, map, false));
            if (this.castShadow)
            {
                MoteMaker.MakeStaticMote(this.ExactPosition, map, ThingDefOf.Mote_ShotHit_Dirt, 1f);
                Effecter effect = this.projectileProperties.effectGroundHit.Spawn();
                effect.Trigger(this,this);
                if (base.Position.GetTerrain(map).takeSplashes)
                {
                    MoteMaker.MakeWaterSplash(this.ExactPosition, map, Mathf.Sqrt((float)this.def.projectile.GetDamageAmount(this.launcher, null)) * 1f, 4f);
                }
            }
            IL_2A6:
            // Either PENETRATION or RICOCHET or STOP can happen.
            CombatEffectsCE.ImpactType impactType = CombatEffectsCE.ImpactHelper.determineImpactType(this, hitThing, ref energyRemaining, isDeflectedByPawn);

            // DECREASE ENERGY
            if (impactType == CombatEffectsCE.ImpactType.PEN && energyRemaining > 0f)
            {
                if (projectileProperties.ammoType == CombatEffectsCE.AmmoType.AP
                    || projectileProperties.ammoType == CombatEffectsCE.AmmoType.API
                    || projectileProperties.ammoType == CombatEffectsCE.AmmoType.SABOT)
                {
                    shotSpeed *= 0.9f;
                    impactSpeedChanged();
                    
                }
                else
                {
                    shotSpeed *= 0.8f;
                    impactSpeedChanged();
                }
            }

            // EITHER STOP or GROUND HIT
            this.landed = (impactType == CombatEffectsCE.ImpactType.STOP || (this.ticksToImpact <= 0) || energyRemaining <= 0f);

            this.ImpactBase(hitThing, this.landed);
        }
        
        private void ImpactBase(Thing hitThing, bool destroyBullet = true)
        {
            CompExplosiveCE compExplosiveCE = this.TryGetComp<CompExplosiveCE>();
            if (compExplosiveCE != null && this.ExactPosition.ToIntVec3().IsValid)
            {
                compExplosiveCE.Explode(this.launcher, this.ExactPosition, base.Map, 1f);
            }
            if (Controller.settings.EnableAmmoSystem && compExplosiveCE == null && base.Position.IsValid && this.def.projectile.preExplosionSpawnChance > 0f && this.def.projectile.preExplosionSpawnThingDef != null && Rand.Value < this.def.projectile.preExplosionSpawnChance)
            {
                ThingDef preExplosionSpawnThingDef = this.def.projectile.preExplosionSpawnThingDef;
                if (preExplosionSpawnThingDef.IsFilth && base.Position.Walkable(base.Map))
                {
                    FilthMaker.TryMakeFilth(base.Position, base.Map, preExplosionSpawnThingDef, 1);
                }
                else if (Controller.settings.ReuseNeolithicProjectiles)
                {
                    Thing thing = ThingMaker.MakeThing(preExplosionSpawnThingDef, null);                    
                    thing.stackCount = 1;
                    thing.SetForbidden(true, false);
                    GenPlace.TryPlaceThing(thing, base.Position, base.Map, ThingPlaceMode.Near, null, null);
                    LessonAutoActivator.TeachOpportunity(CE_ConceptDefOf.CE_ReusableNeolithicProjectiles, thing, OpportunityType.GoodToKnow);
                }
            }
            if (this.def.projectile.explosionRadius > 0f && this.ExactPosition.y < 3f)
            {
                foreach (Thing thing2 in GenRadial.RadialDistinctThingsAround(this.ExactPosition.ToIntVec3(), base.Map, 3f + this.def.projectile.explosionRadius, true))
                {
                    Pawn pawn = thing2 as Pawn;
                    if (pawn != null)
                    {
                        this.ApplySuppression(pawn);
                    }
                }
            }

            if (destroyBullet && !Destroyed)
            {
                this.Destroy(DestroyMode.Vanish);
            }
        }

        private bool CheckCellForCollision(IntVec3 cell)
        {
            bool flag = false;
            bool justWallsRoofs = false;
            float num = (float)(cell - this.OriginIV3).LengthHorizontalSquared;
            if ((!this.def.projectile.alwaysFreeIntercept && this.minCollisionSqr <= 1f) ? (num < 1f) : (num < Mathf.Min(144f, this.minCollisionSqr / 4f)))
            {
                justWallsRoofs = true;
            }
            List<Thing> list = new List<Thing>(base.Map.thingGrid.ThingsListAtFast(cell)).Where(delegate (Thing t)
            {
                if (!justWallsRoofs)
                {
                    return t is Pawn || t.def.Fillage > FillCategory.None;
                }
                return t.def.Fillage == FillCategory.Full;
            }).ToList<Thing>();
            if (!justWallsRoofs)
            {
                List<IntVec3> list2 = new List<IntVec3>();
                list2.AddRange(GenAdj.CellsAdjacentCardinal(cell, Rot4.FromAngleFlat(this.shotRotation), new IntVec2(5, 0)).ToList<IntVec3>());
                foreach (IntVec3 intVec in list2)
                {
                    if (intVec != cell && intVec.InBounds(base.Map))
                    {
                        list.AddRange(from x in base.Map.thingGrid.ThingsListAtFast(intVec)
                                      where x is Pawn
                                      select x);
                        if (debugDrawIntercepts)
                        {
                            base.Map.debugDrawer.FlashCell(intVec, 0.7f, null, 50);
                        }
                    }
                }
            }
            if (this.LastPos.y > 2f)
            {
                if (this.TryCollideWithRoof(cell))
                {
                    return true;
                }
                flag = true;
            }
            foreach (Thing thing in from x in list.Distinct<Thing>() orderby (x.DrawPos - this.LastPos).sqrMagnitude select x)
            {
                // Modify : Added check to check if we hit this thing already than keep searching. Probably I will end up removing the similar check from Impact()
                if ((thing != this.launcher && thing != this.mount) || this.canTargetSelf)
                {
                    if (this.TryCollideWith(thing))
                    {
                        return true;
                    }
                    if (!justWallsRoofs && this.ExactPosition.y < 3f)
                    {
                        Pawn pawn = thing as Pawn;
                        if (pawn != null)
                        {
                            this.ApplySuppression(pawn);
                        }
                    }
                }
            }
            return !flag && this.TryCollideWithRoof(cell);
        }

        private bool CheckForCollisionBetween()
        {
            IntVec3 intVec = this.LastPos.ToIntVec3();
            IntVec3 intVec2 = this.ExactPosition.ToIntVec3();
            if (!intVec.InBounds(base.Map) || !intVec2.InBounds(base.Map))
            {
                return false;
            }
            if (debugDrawIntercepts)
            {
                base.Map.debugDrawer.FlashLine(intVec, intVec2, 50, SimpleColor.White);
            }
            foreach (IntVec3 intVec3 in from x in GenSight.PointsOnLineOfSight(intVec, intVec2).Union(new IntVec3[]
            {
                intVec,
                intVec2
            }).Distinct<IntVec3>()
                                        orderby (x.ToVector3Shifted() - this.LastPos).MagnitudeHorizontalSquared()
                                        select x)
            {
                if (this.CheckCellForCollision(intVec3))
                {
                    return true;
                }
                if (debugDrawIntercepts)
                {
                    base.Map.debugDrawer.FlashCell(intVec3, 1f, "o", 50);
                }
            }
            return false;
        }

        public override void Tick()
        {
            // Lazy initialization
            if (this.projectileProperties == null)
            {
                this.projectileProperties = this.def.projectile as ProjectilePropertiesWithEffectsCE;
            }

            // Try to bypass the ProjectileCE.Tick() which is overriden here.
            // Since it's first thing is to check if landed is true and just returns if it IS we try to trick it.
            bool real_landed_val = landed;
            landed = true;
            base.Tick();
            landed = real_landed_val;

            // This is the same check mention above
            if (this.landed)
            {
                if (!this.Destroyed)
                {
                    this.Destroy(DestroyMode.Vanish);
                }
                return;
            }
            this.LastPos = this.ExactPosition;            
            this.ticksToImpact--; // this increments our position along the trajectory!
            if (!this.ExactPosition.InBounds(base.Map))
            {
                base.Position = this.LastPos.ToIntVec3();
                this.Destroy(DestroyMode.Vanish);
                return;
            }
            if (this.ticksToImpact >= 0 && !this.def.projectile.flyOverhead && this.CheckForCollisionBetween())
            {
                return;
            }
            base.Position = this.ExactPosition.ToIntVec3();
            if (this.ticksToImpact == 60 && Find.TickManager.CurTimeSpeed == TimeSpeed.Normal && this.def.projectile.soundImpactAnticipate != null)
            {
                this.def.projectile.soundImpactAnticipate.PlayOneShot(this);
            }
            if (this.ticksToImpact <= 0)
            {
                this.ImpactSomething();
                return;
            }
            if (this.ambientSustainer != null)
            {
                this.ambientSustainer.Maintain();
            }
        }

        private bool TryCollideWith(Thing thing)
        {
            // if hit thing is it's own launcher and can't hit it self -> No hit!
            if (thing == this.launcher && !this.canTargetSelf || thing == lastThingHit)
            {
                return false;
            }
            float num;
            // IntersectRay method of a Bound intersects the bounding box with the ray and gives us the distance to that point. If it returns false or null .. the ray missed the bound entirely
            if (!CE_Utility.GetBoundsFor(thing).IntersectRay(this.ShotLine, out num))
            {
                return false;
            }
            // If distance square is greater than the distance square we traveled since last tick (this point and the last) -> No hit!
            if (num * num > this.ExactMinusLastPos.sqrMagnitude)
            {
                return false;
            }
            if (thing is Plant)
            {
                // Compute the chance that projectile hits the plant. Based on fillPercentage of plant and something.
                // this.OriginIV3 is the origin of the shot the gun it self.
                // I think [((thing.Position - this.OriginIV3).LengthHorizontal / 40f * this.AccuracyFactor] wanted a parentheses around (40f * this.AccuracyFactor) because now accuracy increases the chance
                float num2 = this.def.projectile.alwaysFreeIntercept ? 1f : ((thing.Position - this.OriginIV3).LengthHorizontal / (40f * this.AccuracyFactor));
                float chance = thing.def.fillPercent * num2;
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
            Vector3 point = this.ShotLine.GetPoint(num);
            if (!point.InBounds(base.Map))
            {
                Log.Error(string.Concat(new object[]
                {
                "TryCollideWith out of bounds point from ShotLine: obj ",
                thing.ThingID,
                ", proj ",
                base.ThingID,
                ", dist ",
                num,
                ", point ",
                point
                    }), false);
            }
            this.ExactPosition = point;

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

            this.Impact(thing);
            return true;
        }

        private bool TryCollideWithRoof(IntVec3 cell)
        {
            if (!cell.Roofed(base.Map))
            {
                return false;
            }
            float num;
            if (!CE_Utility.GetBoundsFor(cell, cell.GetRoof(base.Map)).IntersectRay(this.ShotLine, out num))
            {
                return false;
            }
            if (num * num > this.ExactMinusLastPos.sqrMagnitude)
            {
                return false;
            }
            Vector3 point = this.ShotLine.GetPoint(num);

            this.ExactPosition = point;
            this.landed = true;
            if (debugDrawIntercepts)
            {
                MoteMaker.ThrowText(cell.ToVector3Shifted(), base.Map, "x", Color.red, -1f);
            }
            this.Impact(null);
            return true;
        }

        private void ImpactSomething()
        {
            IntVec3 intVec = this.ExactPosition.ToIntVec3();
            if (this.def.projectile.flyOverhead)
            {
                RoofDef roofDef = base.Map.roofGrid.RoofAt(intVec);
                if (roofDef != null)
                {
                    if (roofDef.isThickRoof)
                    {
                        this.def.projectile.soundHitThickRoof.PlayOneShot(new TargetInfo(intVec, base.Map, false));
                        this.Destroy(DestroyMode.Vanish);
                        return;
                    }
                    if (intVec.GetEdifice(base.Map) == null || intVec.GetEdifice(base.Map).def.Fillage != FillCategory.Full)
                    {
                        RoofCollapserImmediate.DropRoofInCells(intVec, base.Map, null);
                    }
                }
            }
            Thing firstPawn = intVec.GetFirstPawn(base.Map);
            if (firstPawn != null && this.TryCollideWith(firstPawn))
            {
                return;
            }
            List<Thing> list = (from t in base.Map.thingGrid.ThingsListAt(intVec)
                                where t is Pawn || t.def.Fillage > FillCategory.None
                                select t).ToList<Thing>();
            if (list.Count > 0)
            {
                foreach (Thing thing in list)
                {
                    if (this.TryCollideWith(thing))
                    {
                        return;
                    }
                }
            }
            this.ExactPosition = this.ExactPosition;
            this.landed = true;
            this.Impact(null);
        }

        /// -------------------------------------LAUNCH RELATED STUFF-------------------------------------------- ///

        public override void Launch(Thing launcher, Vector2 origin, float shotAngle, float shotRotation, float shotHeight = 0f, float shotSpeed = -1f, Thing equipment = null)
        {
            this.shotAngle = shotAngle;
            this.shotHeight = shotHeight;
            this.shotRotation = shotRotation;
            this.Launch(launcher, origin, equipment);
            if (shotSpeed > 0f)
            {
                this.shotSpeed = shotSpeed;
            }
            this.ticksToImpact = this.IntTicksToImpact;
        }

        // Token: 0x060002A2 RID: 674 RVA: 0x0001646C File Offset: 0x0001466C
        public override void Launch(Thing launcher, Vector2 origin, Thing equipment = null)
        {
            this.shotSpeed = this.def.projectile.speed;
            this.launcher = launcher;
            this.origin = origin;
            this.equipmentDef = ((equipment != null) ? equipment.def : null);
            if (!this.def.projectile.soundAmbient.NullOrUndefined())
            {
                SoundInfo info = SoundInfo.InMap(this, MaintenanceType.PerTick);
                this.ambientSustainer = this.def.projectile.soundAmbient.TrySpawnSustainer(info);
            }
        }

        private void impactSpeedChanged()
        {
            this.origin = new Vector2(this.impactPosition.x,this.impactPosition.z);            
            this.shotHeight = this.Height;
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
                if (this.landed)
                {
                    return this.impactPosition;
                }
                Vector2 vector = this.Vec2Position(-1f);
                return new Vector3(vector.x, this.Height, vector.y);
            }
            set
            {
                this.impactPosition = new Vector3(value.x, value.y, value.z);
                base.Position = this.impactPosition.ToIntVec3();                
            }
        }

        public override Vector3 DrawPos
{
	get
	{
		Vector2 drawPosV = this.DrawPosV2;
		return new Vector3(drawPosV.x, this.def.Altitude, drawPosV.y);
	}
}

        public new Vector2 DrawPosV2
        {
            get
            {
                return this.Vec2Position(-1f) + new Vector2(0f, this.Height - this.shotHeight * ((this.StartingTicksToImpact - this.fTicks) / this.StartingTicksToImpact));
            }
        }

        private Vector2 Vec2Position(float ticks = -1f)
        {
            if (ticks < 0f)
            {
                ticks = this.fTicks;                
            }
            if (ticks != StartingTicksToImpact)
            {
                return Vector2.Lerp(this.origin, this.Destination, ticks / this.StartingTicksToImpact);
            }
            else
            {
                // I had to treat this special case because of 0/0 = NaN problems
                return Vector2.Lerp(this.origin, this.Destination, 1f);
            }
        }

        protected new int FlightTicks
        {
            get
            {
                return this.IntTicksToImpact - this.ticksToImpact;
            }
        }

        protected new float fTicks
        {
            get
            {
                if (this.ticksToImpact != 0)
                {
                    return (float)this.FlightTicks;
                }
                return this.StartingTicksToImpact;
            }
        }

        protected new int IntTicksToImpact
        {
            get
            {
                if (this.intTicksToImpact < 0)
                {
                    this.intTicksToImpact = Mathf.CeilToInt(this.StartingTicksToImpact);
                }
                return this.intTicksToImpact;
            }
        }

        protected new float StartingTicksToImpact
        {
            get
            {
                if (this.startingTicksToImpactInt < 0f)
                {
                    if (this.shotHeight < 0.001f)
                    {
                        if (this.shotAngle < 0f)
                        {
                            this.destinationInt = this.origin;
                            this.startingTicksToImpactInt = 0f;
                            this.ImpactSomething();
                            return 0f;
                        }
                        this.startingTicksToImpactInt = (this.origin - this.Destination).magnitude / (Mathf.Cos(this.shotAngle) * this.shotSpeed) * 60f;
                        return this.startingTicksToImpactInt;
                    }
                    else
                    {
                        this.startingTicksToImpactInt = this.GetFlightTime() * 60f;
                    }
                }
                return this.startingTicksToImpactInt;
            }
        }

        private float GetFlightTime()
        {
            return (Mathf.Sin(this.shotAngle) * this.shotSpeed + Mathf.Sqrt(Mathf.Pow(Mathf.Sin(this.shotAngle) * this.shotSpeed, 2f) + 2f * this.GravityFactor * this.shotHeight)) / this.GravityFactor;
        }

        private float GetHeightAtTicks(int ticks)
        {
            float num = (float)ticks / 60f;
            return (float)Math.Round((double)(this.shotHeight + this.shotSpeed * Mathf.Sin(this.shotAngle) * num - this.GravityFactor * num * num / 2f), 3);
        }

        public new float Height
        {
            get
            {
                if (this.lastHeightTick != this.FlightTicks)
                {
                    this.heightInt = ((this.ticksToImpact > 0) ? this.GetHeightAtTicks(this.FlightTicks) : 0f);
                    this.lastHeightTick = this.FlightTicks;
                }
                return this.heightInt;
            }
        }

        private float GravityFactor
        {
            get
            {
                if (this._gravityFactor < 0f)
                {
                    this._gravityFactor = 9.8f;
                    ProjectilePropertiesCE projectilePropertiesCE = this.def.projectile as ProjectilePropertiesCE;
                    if (projectilePropertiesCE != null)
                    {
                        this._gravityFactor = projectilePropertiesCE.Gravity;
                    }
                }
                return this._gravityFactor;
            }
        }

        private Vector3 LastPos
        {
            get
            {
                if (this.lastExactPos.x < -999f)
                {
                    Vector2 vector = this.Vec2Position((float)(this.FlightTicks - 1));
                    this.lastExactPos = new Vector3(vector.x, this.GetHeightAtTicks(this.FlightTicks - 1), vector.y);
                }
                return this.lastExactPos;
            }
            set
            {
                this.lastExactPos = value;
            }
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
        private int lastShotLine = -1;
        private Vector3 lastExactPos = new Vector3(-1000f, 0f, 0f);
        private float _gravityFactor = -1f;
        private Sustainer ambientSustainer;
        private float suppressionAmount;
        private float energyRemaining = 100f;
        private Vector3 impactPosition;
        private IntVec3 originInt = new IntVec3(0, -1000, 0);
        private int lastHeightTick = -1;
        private float heightInt;
        private static List<IntVec3> checkedCells = new List<IntVec3>();


    }
}
