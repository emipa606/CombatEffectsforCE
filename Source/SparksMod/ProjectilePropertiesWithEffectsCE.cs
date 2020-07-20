using System;
using System.Collections.Generic;
using Verse;
using CombatEffectsCE;

namespace CombatExtended
{
    // Token: 0x02000079 RID: 121
    public class ProjectilePropertiesWithEffectsCE : ProjectilePropertiesCE
    {
        //public List<EffecterDef> effectsWallHit;
        public EffecterDef effectStoneWallHit;
        public EffecterDef effectWoodWallHit;
        public EffecterDef effectBloodHit;
        public EffecterDef effectGroundHit;
        public EffecterDef effectBuildingBits;
        public EffecterDef effectPuff;
        public AmmoType ammoType = AmmoType.UNDEFINED;
        public Caliber caliber = Caliber.UNDEFINED;
    }
}
