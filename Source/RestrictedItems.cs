using System.Collections.Generic;
using RimWorld;
using Verse;

namespace ItemRequests
{
    public class RestrictedItems
    {
        private static List<ThingDef> thingDefs = new List<ThingDef>();
        public static List<ThingDef> Get => thingDefs;
        public static void Add(ThingDef def) => thingDefs.Add(def);
        public static void Remove(ThingDef def) => thingDefs.Remove(def);
        public static bool Contains(ThingDef def) => thingDefs.Contains(def);

        // TODO: learn how to add to option menu in game for modifying
        public static void Init()
        {
            thingDefs.Add(ThingDefOf.AIPersonaCore);
            thingDefs.Add(ThingDefOf.TechprofSubpersonaCore);
            thingDefs.Add(ThingDefOf.VanometricPowerCell);
            thingDefs.Add(ThingDefOf.InfiniteChemreactor);
            thingDefs.Add(ThingDefOf.PsychicEmanator);
            thingDefs.Add(ThingDefOf.Thrumbo);
            thingDefs.Add(ThingDefOf.PowerBeam);
            thingDefs.Add(ThingDefOf.OrbitalTargeterBombardment);
            thingDefs.Add(ThingDefOf.OrbitalTargeterPowerBeam);
        }
    }
}
