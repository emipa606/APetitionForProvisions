using HarmonyLib;
using Verse;

namespace ItemRequests;

[HarmonyPatch(typeof(Game), nameof(Game.UpdatePlay))]
public static class DatabaseAsyncLoaderLoadedGame
{
    [HarmonyPostfix]
    public static void LoadDBFrame()
    {
        ThingDatabase.Instance.LoadFrame();
    }
}