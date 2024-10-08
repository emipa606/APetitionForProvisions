using Verse;

namespace ItemRequests;

/// <summary>
///     Definition of the settings for the mod
/// </summary>
internal class ItemRequestsSettings : ModSettings
{
    public float ApparelMultiplier = 1f;
    public float PriceMultiplier = 0.5f;
    public float WeaponMultiplier = 2f;

    /// <summary>
    ///     Saving and loading the values
    /// </summary>
    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref PriceMultiplier, "PriceMultiplier", 0.5f);
        Scribe_Values.Look(ref ApparelMultiplier, "ApparelMultiplier", 1f);
        Scribe_Values.Look(ref WeaponMultiplier, "WeaponMultiplier", 2f);
    }

    public void Reset()
    {
        PriceMultiplier = 0.5f;
        ApparelMultiplier = 1f;
        WeaponMultiplier = 2f;
    }
}