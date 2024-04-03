using System.Reflection;
using HarmonyLib;
using Verse;

namespace ItemRequests;

[StaticConstructorOnStartup]
public class ItemRequests
{
    static ItemRequests()
    {
        new Harmony("com.github.toywalrus.itemrequests").PatchAll(Assembly.GetExecutingAssembly());
        RestrictedItems.Init();
    }
}