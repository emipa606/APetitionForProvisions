using System;
using System.Collections.Generic;
using Verse;

namespace ItemRequests;

// These classes were adapted from the classes in CostCalculator.cs
// from the mod EdB Prepare Carefully by edbmods
// https://github.com/edbmods/EdBPrepareCarefully/blob/develop/Source/CostCalculator.cs
public class CostCalculator
{
    private readonly HashSet<string> cheapApparel = [];
    protected HashSet<string> freeApparel = [];

    public CostCalculator()
    {
        cheapApparel.Add("Apparel_Pants");
        cheapApparel.Add("Apparel_BasicShirt");
        cheapApparel.Add("Apparel_Jacket");
    }

    public void CalculatePawnCost(ColonistCostDetails cost, Pawn pawn)
    {
        cost.Clear();
        cost.name = pawn.Name.ToString();

        // Start with the market value plus a bit of a mark-up.
        cost.marketValue = pawn.MarketValue;
        cost.marketValue += 300;

        // Calculate passion cost.  Each passion above 8 makes all passions
        // cost more.  Minor passion counts as one passion.  Major passion
        // counts as 3.
        // double skillCount = pawn.currentPassions.Keys.Count();
        double passionLevelCount = 0;
        double passionLevelCost = 20;
        var levelCost = passionLevelCost;
        if (passionLevelCount > 8)
        {
            var penalty = passionLevelCount - 8;
            levelCost += penalty * 0.4;
        }

        cost.marketValue += levelCost * passionLevelCount;

        cost.apparel = Math.Ceiling(cost.apparel);
        cost.bionics = Math.Ceiling(cost.bionics);

        // Use a multiplier to balance pawn cost vs. equipment cost.
        // Disabled for now.
        cost.Multiply(1.0);

        cost.ComputeTotal();
    }

    public double CalculateStackCost(ThingDef def, double baseCost)
    {
        var cost = baseCost;

        if (def.MadeFromStuff)
        {
            if (def.IsApparel)
            {
                cost *= ItemRequestsMod.instance.Settings.ApparelMultiplier;
            }
            else
            {
                cost *= ItemRequestsMod.instance.Settings.PriceMultiplier;
            }
        }

        if (def.IsRangedWeapon)
        {
            cost *= ItemRequestsMod.instance.Settings.WeaponMultiplier;
        }

        cost = Math.Round(cost, 1);

        return cost;
    }

    public double CalculateThingCost(ThingKey thingKey)
    {
        var entry = ThingDatabase.Instance.LookupThingEntry(thingKey);
        if (entry != null)
        {
            return entry.cost;
        }

        return 0;
    }

    public double GetBaseThingCost(ThingDef def, ThingDef stuffDef)
    {
        if (def == null)
        {
            Log.Warning("Trying to calculate the cost of a null ThingDef");
            return 0;
        }

        if (!(def.BaseMarketValue > 0))
        {
            return 0;
        }

        if (stuffDef == null)
        {
            return def.BaseMarketValue;
        }

        if (def.thingClass == null)
        {
            Log.Warning($"Trying to calculate the cost of a ThingDef with null thingClass: {def.defName}");
            return 0;
        }

        try
        {
            var thing = ThingMaker.MakeThing(def, stuffDef);
            if (thing != null)
            {
                return thing.MarketValue;
            }

            Log.Warning($"Failed when calling MakeThing({def.defName}, ...) to calculate a ThingDef's market value");
            return 0;
        }
        catch (Exception e)
        {
            Log.Warning($"Failed to calculate the cost of a ThingDef ({def.defName}): ");
            Log.Warning(e.ToString());
            return 0;
        }
    }
}