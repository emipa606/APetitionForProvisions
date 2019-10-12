using RimWorld;
using Verse;
using Harmony;

namespace ItemRequests
{
    [HarmonyPatch(typeof(FactionDialogMaker))]
    [HarmonyPatch("FactionDialogFor")]
    public static class NegotiationOption
    {
        
        [HarmonyPostfix]
        public static void AddOption(DiaNode __instance, ref DiaNode __result, Pawn negotiator, Faction faction)
        {
            Log.Message("Talking to " + faction.Name);
            
            DiaOption diaOption = new DiaOption("My custom option!");
            diaOption.resolveTree = true;
            
            __result.options.Add(diaOption);
        }
    }
}