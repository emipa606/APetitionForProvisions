using System.Reflection;
using Verse;
using HarmonyLib;

namespace ItemRequests
{
    [StaticConstructorOnStartup]
    public class ItemRequests
    {
        static ItemRequests()
        {
            var harmony = new Harmony("com.github.toywalrus.itemrequests");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            RestrictedItems.Init();
        }       
    }

    // This should incrementally load the database
    // so that when the player opens the comms window
    // for the first time, it's not super bogged
    // down by loading every item then.
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