using HarmonyLib;
using Verse;

namespace ItemRequests.HarmonyPatches;

[HarmonyPatch(typeof(Game), nameof(Game.UpdatePlay))]
public static class Game_UpdatePlay
{
    public static void Postfix()
    {
        ThingDatabase.Instance.LoadFrame();
    }
}