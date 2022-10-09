using Verse;

namespace ItemRequests;

public static class ThingTypeExtension
{
    public static bool HasQuality(this ThingType t)
    {
        return t is ThingType.Apparel or ThingType.Buildings or ThingType.Weapons;
    }

    public static string Translate(this ThingType t)
    {
        return ("IR.ThingType." + t).Translate();
    }
}