using System.Reflection;
using HarmonyLib;
using Verse;

namespace ItemRequests;

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