using System;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;

namespace CombatEffectsCE
{
    // Token: 0x02000E46 RID: 3654

    public class MoteFilthy : MoteThrown
    {

        protected override void TimeInterval(float deltaTime)
        {
            base.TimeInterval(deltaTime);
            //float alpha = this.Alpha;
            //Graphic.color.a = alpha;

            //Graphic.MatSingle.color = Graphic.Color;
            //Graphic.MatSingle.SetColor(ShaderPropertyIDs.Color, Graphic.Color);
         
            if (base.Destroyed)
            {
                return;
            }
            if (!this.Flying && !this.Skidding)
            {
                return;
            }
            Vector3 vector = this.NextExactPosition(deltaTime);
            IntVec3 intVec = new IntVec3(vector);
            if (intVec != base.Position)
            {
                if (!intVec.InBounds(base.Map))
                {
                    this.Destroy(DestroyMode.Vanish);
                    return;
                }
                if (this.def.mote.collide && intVec.Filled(base.Map))
                {
                    this.WallHit();
                    return;
                }
            }
            base.Position = intVec;
            this.exactPosition = vector;
            if (this.def.mote.rotateTowardsMoveDirection && this.velocity != default(Vector3))
            {
                this.exactRotation = this.velocity.AngleFlat();
            }
            else
            {
                this.exactRotation += this.rotationRate * deltaTime;
            }
            this.velocity += this.def.mote.acceleration * deltaTime;
            if (this.def.mote.speedPerTime != 0f)
            {
                this.Speed = Mathf.Max(this.Speed + this.def.mote.speedPerTime * deltaTime, 0f);
            }
            if (this.airTimeLeft > 0f)
            {
                this.airTimeLeft -= deltaTime;
                if (this.airTimeLeft < 0f)
                {
                    this.airTimeLeft = 0f;
                }
                if (this.airTimeLeft <= 0f && !this.def.mote.landSound.NullOrUndefined())
                {
                    this.def.mote.landSound.PlayOneShot(new TargetInfo(base.Position, base.Map, false));
                }
            }
            if (this.Skidding)
            {
                this.Speed *= this.skidSpeedMultiplierPerTick;
                this.rotationRate *= this.skidSpeedMultiplierPerTick;
                if (this.Speed < 0.02f)
                {
                    this.Speed = 0f;
                }
                float rng = UnityEngine.Random.Range(0f, 1f);
                if (rng < 0.3f)
                {
                    FilthMaker.TryMakeFilth(intVec, this.Map, ((MotePropertiesFilthy)this.def.mote).filthTrace, 1);
                }
            }
        }
    }
}
