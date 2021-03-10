using CombatEffectsCE;
using Verse;

namespace CombatExtended
{
    // Token: 0x02000079 RID: 121
    public class ProjectilePropertiesWithEffectsCE : ProjectilePropertiesCE
    {
        public readonly AmmoType ammoType = AmmoType.UNDEFINED;
        public readonly Caliber caliber = Caliber.UNDEFINED;
        public EffecterDef effectBloodHit;
        public EffecterDef effectBuildingBits;
        public EffecterDef effectGroundHit;

        public EffecterDef effectPuff;

        //public List<EffecterDef> effectsWallHit;
        public EffecterDef effectStoneWallHit;
        public EffecterDef effectWoodWallHit;
    }
}