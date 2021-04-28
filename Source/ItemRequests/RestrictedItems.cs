using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace ItemRequests
{
    public class RestrictedItems
    {
        public static Dictionary<ThingDef, TechLevel> researchTechCache = new Dictionary<ThingDef, TechLevel>();
        public static List<ThingDef> GetThings { get; } = new List<ThingDef>();

        public static List<PawnKindDef> GetPawns { get; } = new List<PawnKindDef>();

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

            var validThings = from thing in DefDatabase<ThingDef>.AllDefsListForReading
                where thing.recipeMaker != null || thing.researchPrerequisites != null
                select thing;
            Log.Message("[A Petition For Provisions] Caching tech-level for things");
            foreach (var validThing in validThings)
            {
                var currentTechLevel = TechLevel.Neolithic;
                if (validThing.researchPrerequisites?.Count > 0)
                {
                    foreach (var validThingResearchPrerequisite in validThing.researchPrerequisites)
                    {
                        if (validThingResearchPrerequisite.techLevel > currentTechLevel)
                        {
                            currentTechLevel = validThingResearchPrerequisite.techLevel;
                        }
                    }
                }

                if (validThing.recipeMaker?.researchPrerequisites?.Count > 0)
                {
                    foreach (var validThingResearchPrerequisite in validThing.recipeMaker.researchPrerequisites)
                    {
                        if (validThingResearchPrerequisite.techLevel > currentTechLevel)
                        {
                            currentTechLevel = validThingResearchPrerequisite.techLevel;
                        }
                    }
                }

                if (validThing.recipeMaker?.researchPrerequisite?.techLevel > currentTechLevel)
                {
                    currentTechLevel = validThing.recipeMaker.researchPrerequisite.techLevel;
                }

                researchTechCache.Add(validThing, currentTechLevel);
            }

            Log.Message("[A Petition For Provisions] Caching complete");
        }
    }
}