using System.Linq;
using CombatExtended;
using Verse;

namespace CombatEffectsCE;

[StaticConstructorOnStartup]
public class Main
{
    public static readonly bool VehiclesLoaded;

    static Main()
    {
        VehiclesLoaded = ModLister.GetActiveModWithIdentifier("SmashPhil.VehicleFramework") != null;

        foreach (var thingDef in DefDatabase<ThingDef>.AllDefsListForReading.Where(def =>
                     def.thingClass == typeof(BulletCESparky) &&
                     def.defName.StartsWith("Bullet_") && def.defName.Contains("Shell_")))
        {
            thingDef.thingClass = typeof(BulletCE);
            CombatEffectsCEMod.LogMessage(
                $"{thingDef.defName} changed back to normal CE-bullet as mortar-ammo should not be changed");
        }
    }
}