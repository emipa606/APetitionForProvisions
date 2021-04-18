using Verse;

namespace ItemRequests
{
    public static class ThingTypeExtension
    {
        public static bool HasQuality(this ThingType t)
        {
            return t == ThingType.Apparel || t == ThingType.Buildings || t == ThingType.Weapons;
        }

        public static string Translate(this ThingType t)
        {
            return ("IR.ThingType." + t).Translate();
        }
    }
}