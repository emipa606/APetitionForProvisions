using Verse;

namespace ItemRequests;

public static class ThingTypeExtension
{
    extension(ThingType t)
    {
        public bool HasQuality()
        {
            return t is ThingType.Apparel or ThingType.Buildings or ThingType.Weapons;
        }

        public string Translate()
        {
            return ("IR.ThingType." + t).Translate();
        }
    }
}