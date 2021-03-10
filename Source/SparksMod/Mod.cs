using UnityEngine;
using Verse;

namespace CombatEffectsCE
{
    [StaticConstructorOnStartup]
    internal class CombatEffectsCEMod : Mod
    {
        /// <summary>
        ///     The instance of the settings to be read by the mod
        /// </summary>
        public static CombatEffectsCEMod instance;

        /// <summary>
        ///     The private settings
        /// </summary>
        private CombatEffectsCESettings settings;

        /// <summary>
        ///     Cunstructor
        /// </summary>
        /// <param name="content"></param>
        public CombatEffectsCEMod(ModContentPack content) : base(content)
        {
            instance = this;
        }

        /// <summary>
        ///     The instance-settings for the mod
        /// </summary>
        internal CombatEffectsCESettings Settings
        {
            get
            {
                if (settings == null)
                {
                    settings = GetSettings<CombatEffectsCESettings>();
                }

                return settings;
            }
            set => settings = value;
        }

        /// <summary>
        ///     The title for the mod-settings
        /// </summary>
        /// <returns></returns>
        public override string SettingsCategory()
        {
            return "Combat Effects for Combat Extended";
        }

        /// <summary>
        ///     The settings-window
        ///     For more info: https://rimworldwiki.com/wiki/Modding_Tutorials/ModSettings
        /// </summary>
        /// <param name="rect"></param>
        public override void DoSettingsWindowContents(Rect rect)
        {
            var listing_Standard = new Listing_Standard();
            listing_Standard.Begin(rect);
            listing_Standard.CheckboxLabeled("SettingExtraBlood".Translate(), ref Settings.ExtraBlood,
                "SettingExtraBloodDescription".Translate());
            //listing_Standard.CheckboxLabeled("SettingPenetrationMechanics".Translate(),
            //    ref Settings.PenetrationMechanics, "SettingPenetrationMechanicsDescription".Translate());
            listing_Standard.End();
            Settings.Write();
        }
    }
}