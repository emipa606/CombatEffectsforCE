using UnityEngine;

namespace CombatExtended;

internal static class My_CE_Utility
{
    public static float MaxProjectileRange(float shotHeight, float shotSpeed, float shotAngle, double gravityFactor)
    {
        if (shotHeight < 0.001f)
        {
            return (float)(Mathf.Pow(shotSpeed, 2f) / gravityFactor * Mathf.Sin(2f * shotAngle));
        }

        return (float)(shotSpeed * Mathf.Cos(shotAngle) / gravityFactor * ((shotSpeed * Mathf.Sin(shotAngle)) +
                                                                           Mathf.Sqrt((float)(Mathf.Pow(
                                                                                   shotSpeed * Mathf.Sin(shotAngle),
                                                                                   2f) +
                                                                               (2f * gravityFactor * shotHeight)))));
    }
}