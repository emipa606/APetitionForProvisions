using System.Collections.Generic;
using RimWorld;
using Verse;

namespace ItemRequests
{
    public class RestrictedItems
    {
        public static List<ThingDef> GetThings { get; } = new();

        public static List<PawnKindDef> GetPawns { get; } = new();

        public static void Add(ThingDef def)
        {
            GetThings.Add(def);
        }

        public static void Add(PawnKindDef def)
        {
            GetPawns.Add(def);
        }

        public static void Remove(ThingDef def)
        {
            GetThings.Remove(def);
        }

        public static void Remove(PawnKindDef def)
        {
            GetPawns.Remove(def);
        }

        public static bool Contains(ThingDef def)
        {
            return GetThings.Contains(def);
        }

        public static bool Contains(PawnKindDef def)
        {
            return GetPawns.Contains(def);
        }

        // TODO: learn how to add to option menu in game for modifying
        public static void Init()
        {
            GetThings.Add(ThingDefOf.AIPersonaCore);
            GetThings.Add(ThingDefOf.OrbitalTargeterBombardment);
            GetThings.Add(ThingDefOf.OrbitalTargeterPowerBeam);
            GetThings.Add(ThingDefOf.TechprofSubpersonaCore);
            GetThings.Add(ThingDefOf.VanometricPowerCell);
            GetThings.Add(ThingDefOf.InfiniteChemreactor);
            GetThings.Add(ThingDefOf.PsychicEmanator);
            GetThings.Add(ThingDefOf.PowerBeam);

            GetPawns.Add(PawnKindDefOf.Thrumbo);
            GetPawns.Add(PawnKindDefOf.Megascarab);
            GetPawns.Add(PawnKindDefOf.Megaspider);
            GetPawns.Add(PawnKindDefOf.Spelopede);
            GetPawns.Add(PawnKindDefOf.Alphabeaver);
        }
    }
}