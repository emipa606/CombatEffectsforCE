using Mlie;
using UnityEngine;
using Verse;

namespace CombatEffectsCE;

[StaticConstructorOnStartup]
internal class CombatEffectsCEMod : Mod
{
    /// <summary>
    ///     The instance of the settings to be read by the mod
    /// </summary>
    public static CombatEffectsCEMod instance;

    private static string currentVersion;


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
        currentVersion =
            VersionFromManifest.GetVersionFromModMetaData(
                ModLister.GetActiveModWithIdentifier("Mlie.CombatEffectsforCE"));
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
        if (currentVersion != null)
        {
            listing_Standard.Gap();
            GUI.contentColor = Color.gray;
            listing_Standard.Label("CurrentModVersion_Label".Translate(currentVersion));
            GUI.contentColor = Color.white;
        }

        //var labelRect = listing_Standard.Label("CEAmmoSelector_Label".Translate());
        //var frameRect = rect;
        //frameRect.x = 0;
        //frameRect.y = labelRect.y + 20;
        //frameRect.height = rect.height - (labelRect.y - frameRect.y);

        //var contentRect = frameRect;
        //contentRect.x = 0;
        //contentRect.y = labelRect.y + 20;
        //contentRect.width -= 20;

        //contentRect.height = (noneCategoryMembers.Count * 24f) + 40f;
        //Widgets.BeginScrollView(frameRect, ref optionsScrollPosition, contentRect);
        listing_Standard.End();
        Settings.Write();
    }
}