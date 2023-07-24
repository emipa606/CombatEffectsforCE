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
    public readonly CombatEffectsCESettings Settings;

    /// <summary>
    ///     Cunstructor
    /// </summary>
    /// <param name="content"></param>
    public CombatEffectsCEMod(ModContentPack content) : base(content)
    {
        currentVersion =
            VersionFromManifest.GetVersionFromModMetaData(content.ModMetaData);
        instance = this;
        Settings = GetSettings<CombatEffectsCESettings>();
    }

    public static void LogMessage(string message, bool forced = false)
    {
        if (!forced && !instance.Settings.VerboseLogging)
        {
            return;
        }

        Log.Message($"[CombatEffectsCE]: {message}");
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
        listing_Standard.CheckboxLabeled("SparksModVerboseLogging".Translate(), ref Settings.VerboseLogging,
            "SparksModVerboseLoggingDescription".Translate());

        if (currentVersion != null)
        {
            listing_Standard.Gap();
            GUI.contentColor = Color.gray;
            listing_Standard.Label("CurrentModVersion_Label".Translate(currentVersion));
            GUI.contentColor = Color.white;
        }

        listing_Standard.End();
        Settings.Write();
    }
}