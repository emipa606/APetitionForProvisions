using System.Reflection;
using System;
using RimWorld;
using UnityEngine;
using RimWorld.Planet;
using Verse;
using Harmony;

namespace ItemRequests
{
    [StaticConstructorOnStartup]
    public class ItemRequests
    {
        static ItemRequests()
        {
            var harmony = HarmonyInstance.Create("com.github.toywalrus.itemrequests");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }
}

// Faction: GetInfoText() GetCallLabel() 
// Dialog_Negotiation: DoWindowContents()
// Dialog_NodeTree
// Verse.Window