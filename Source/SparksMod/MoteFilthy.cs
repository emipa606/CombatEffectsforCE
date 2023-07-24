using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace CombatEffectsCE;

public class MoteFilthy : MoteThrown
{
    public override void TimeInterval(float deltaTime)
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

        var vector = NextExactPosition(deltaTime);
        var intVec = new IntVec3(vector);
        if (intVec != Position)
        {
            if (!intVec.InBounds(Map))
            {
                Destroy();
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
            Speed = Mathf.Max(Speed + (def.mote.speedPerTime * deltaTime), 0f);
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
                def.mote.landSound.PlayOneShot(new TargetInfo(Position, Map));
            }
        }

        if (!Skidding)
        {
            return;
        }

        Speed *= skidSpeedMultiplierPerTick;
        rotationRate *= skidSpeedMultiplierPerTick;
        if (Speed < 0.02f)
        {
            Speed = 0f;
        }

        var rng = Random.Range(0f, 1f);
        if (rng < 0.3f)
        {
            FilthMaker.TryMakeFilth(intVec, Map, ((MotePropertiesFilthy)def.mote).filthTrace);
        }
    }
}