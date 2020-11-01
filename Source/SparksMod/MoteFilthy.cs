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
         
            if (Destroyed)
            {
                return;
            }
            if (!Flying && !Skidding)
            {
                return;
            }
            Vector3 vector = NextExactPosition(deltaTime);
            IntVec3 intVec = new IntVec3(vector);
            if (intVec != Position)
            {
                if (!intVec.InBounds(Map))
                {
                    Destroy(DestroyMode.Vanish);
                    return;
                }
                if (def.mote.collide && intVec.Filled(Map))
                {
                    WallHit();
                    return;
                }
            }
            Position = intVec;
            exactPosition = vector;
            if (def.mote.rotateTowardsMoveDirection && velocity != default)
            {
                exactRotation = velocity.AngleFlat();
            }
            else
            {
                exactRotation += rotationRate * deltaTime;
            }
            velocity += def.mote.acceleration * deltaTime;
            if (def.mote.speedPerTime != 0f)
            {
                Speed = Mathf.Max(Speed + def.mote.speedPerTime * deltaTime, 0f);
            }
            if (airTimeLeft > 0f)
            {
                airTimeLeft -= deltaTime;
                if (airTimeLeft < 0f)
                {
                    airTimeLeft = 0f;
                }
                if (airTimeLeft <= 0f && !def.mote.landSound.NullOrUndefined())
                {
                    def.mote.landSound.PlayOneShot(new TargetInfo(Position, Map, false));
                }
            }
            if (Skidding)
            {
                Speed *= skidSpeedMultiplierPerTick;
                rotationRate *= skidSpeedMultiplierPerTick;
                if (Speed < 0.02f)
                {
                    Speed = 0f;
                }
                float rng = UnityEngine.Random.Range(0f, 1f);
                if (rng < 0.3f)
                {
                    FilthMaker.TryMakeFilth(intVec, Map, ((MotePropertiesFilthy)def.mote).filthTrace, 1);
                }
            }
        }
    }
}
