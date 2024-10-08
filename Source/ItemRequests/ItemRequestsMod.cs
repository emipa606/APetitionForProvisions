using Mlie;
using UnityEngine;
using Verse;

namespace ItemRequests;

[StaticConstructorOnStartup]
internal class ItemRequestsMod : Mod
{
    /// <summary>
    ///     The instance of the settings to be read by the mod
    /// </summary>
    public static ItemRequestsMod instance;

    private static string currentVersion;

    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="content"></param>
    public ItemRequestsMod(ModContentPack content) : base(content)
    {
        instance = this;
        Settings = GetSettings<ItemRequestsSettings>();
        currentVersion = VersionFromManifest.GetVersionFromModMetaData(content.ModMetaData);
    }

    /// <summary>
    ///     The instance-settings for the mod
    /// </summary>
    internal ItemRequestsSettings Settings { get; }

    /// <summary>
    ///     The title for the mod-settings
    /// </summary>
    /// <returns></returns>
    public override string SettingsCategory()
    {
        return "A Petition for Provisions";
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

        if (listing_Standard.ButtonTextLabeledPct("IR.resetToDefault".Translate(), "IR.reset".Translate(), 0.5f))
        {
            Settings.Reset();
        }

        Settings.PriceMultiplier = listing_Standard.SliderLabeled(
            "IR.priceMultiplier".Translate(Settings.PriceMultiplier.ToStringPercent()), Settings.PriceMultiplier, 0.1f,
            5f,
            tooltip: "IR.priceMultiplierTT".Translate());
        Settings.ApparelMultiplier = listing_Standard.SliderLabeled(
            "IR.apparelMultiplier".Translate(Settings.ApparelMultiplier.ToStringPercent()), Settings.ApparelMultiplier,
            0.1f,
            5f,
            tooltip: "IR.priceMultiplierTT".Translate());
        Settings.WeaponMultiplier = listing_Standard.SliderLabeled(
            "IR.weaponMultiplier".Translate(Settings.WeaponMultiplier.ToStringPercent()), Settings.WeaponMultiplier,
            0.1f,
            5f,
            tooltip: "IR.priceMultiplierTT".Translate());

        if (currentVersion != null)
        {
            listing_Standard.Gap();
            GUI.contentColor = Color.gray;
            listing_Standard.Label("IR.CurrentModVersion".Translate(currentVersion));
            GUI.contentColor = Color.white;
        }

        listing_Standard.End();
    }
}