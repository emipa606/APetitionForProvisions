using System.Collections.Generic;
using RimWorld;
using Verse;

namespace ItemRequests
{
    public class RestrictedItems
    {
        private static List<PawnKindDef> pawnDefs = new List<PawnKindDef>();
        private static List<ThingDef> thingDefs = new List<ThingDef>();
        public static List<ThingDef> GetThings => thingDefs;
        public static List<PawnKindDef> GetPawns => pawnDefs;

        public static void Add(ThingDef def) => thingDefs.Add(def);
        public static void Add(PawnKindDef def) => pawnDefs.Add(def);

        public static void Remove(ThingDef def) => thingDefs.Remove(def);
        public static void Remove(PawnKindDef def) => pawnDefs.Remove(def);

        public static bool Contains(ThingDef def) => thingDefs.Contains(def);
        public static bool Contains(PawnKindDef def) => pawnDefs.Contains(def);

        // TODO: learn how to add to option menu in game for modifying
        public static void Init()
        {
            thingDefs.Add(ThingDefOf.AIPersonaCore);
            thingDefs.Add(ThingDefOf.OrbitalTargeterBombardment);
            thingDefs.Add(ThingDefOf.OrbitalTargeterPowerBeam);
            thingDefs.Add(ThingDefOf.TechprofSubpersonaCore);
            thingDefs.Add(ThingDefOf.VanometricPowerCell);
            thingDefs.Add(ThingDefOf.InfiniteChemreactor);
            thingDefs.Add(ThingDefOf.PsychicEmanator);            
            thingDefs.Add(ThingDefOf.PowerBeam);

            pawnDefs.Add(PawnKindDefOf.Thrumbo);
            pawnDefs.Add(PawnKindDefOf.Megascarab);
            pawnDefs.Add(PawnKindDefOf.Megaspider);
            pawnDefs.Add(PawnKindDefOf.Spelopede);
            pawnDefs.Add(PawnKindDefOf.Alphabeaver);
        }
    }
}
