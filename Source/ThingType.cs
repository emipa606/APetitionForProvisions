using Verse;
namespace ItemRequests
{
    // This enum was adapted from the class EquipmentType
    // from the mod EdB Prepare Carefully by edbmods
    // https://github.com/edbmods/EdBPrepareCarefully/blob/develop/Source/EquipmentType.cs

    public enum ThingType
    {
        //All,
        Resources,
        Food,
        Weapons,
        Apparel,
        Medical,
        Buildings,
        Animals,
        Other,
        Discard
    }

    public static class ThingTypeExtension
    {
        public static string Translate(this ThingType t)
        {
            return ("IR.ThingType." + t.ToString()).Translate();            
        }
        public static bool HasQuality(this ThingType t)
        {
            return t == ThingType.Apparel || t == ThingType.Buildings || t == ThingType.Weapons;
        }
    }
}
