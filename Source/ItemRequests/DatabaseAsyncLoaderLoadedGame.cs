using HarmonyLib;
using Verse;

namespace ItemRequests
{
    [HarmonyPatch(typeof(Game))]
    [HarmonyPatch("UpdatePlay")]
    public static class DatabaseAsyncLoaderLoadedGame
    {
        [HarmonyPostfix]
        public static void LoadDBFrame()
        {
            ThingDatabase.Instance.LoadFrame();
        }
    }
}