using System;
using System.Collections.Generic;
using Verse;

namespace ItemRequests
{
    // These classes were adapted from the classes in CostCalculator.cs
    // from the mod EdB Prepare Carefully by edbmods
    // https://github.com/edbmods/EdBPrepareCarefully/blob/develop/Source/CostCalculator.cs

    public class ColonistCostDetails
    {
        private double animals;
        public double apparel;
        public double bionics;
        public double marketValue;
        public string name;
        public double passionCount = 0;
        private double passions;
        public double total;
        private double traits;

        public void Clear()
        {
            total = 0;
            passions = 0;
            traits = 0;
            apparel = 0;
            bionics = 0;
            animals = 0;
            marketValue = 0;
        }

        public void ComputeTotal()
        {
            total = Math.Ceiling(passions + traits + apparel + bionics + marketValue + animals);
        }

        public void Multiply(double amount)
        {
            passions = Math.Ceiling(passions * amount);
            traits = Math.Ceiling(traits * amount);
            marketValue = Math.Ceiling(marketValue * amount);
            ComputeTotal();
        }
    }

    public class CostDetails
    {
        private readonly List<ColonistCostDetails> colonistDetails = new();
        private double animals;
        private double colonistApparel;
        private double colonistBionics;
        private double colonists;
        private double equipment;
        public Pawn pawn = null;
        private double total;

        public void Clear(int colonistCount)
        {
            total = 0;
            equipment = 0;
            animals = 0;
            colonists = 0;
            colonistApparel = 0;
            colonistBionics = 0;
            var listSize = colonistDetails.Count;
            if (colonistCount == listSize)
            {
                return;
            }

            if (colonistCount < listSize)
            {
                var diff = listSize - colonistCount;
                colonistDetails.RemoveRange(colonistDetails.Count - diff, diff);
            }
            else
            {
                var diff = colonistCount - listSize;
                for (var i = 0; i < diff; i++)
                {
                    colonistDetails.Add(new ColonistCostDetails());
                }
            }
        }

        public void ComputeTotal()
        {
            equipment = Math.Ceiling(equipment);
            animals = Math.Ceiling(animals);
            total = equipment + animals;
            foreach (var cost in colonistDetails)
            {
                total += cost.total;
                colonists += cost.total;
                colonistApparel += cost.apparel;
                colonistBionics += cost.bionics;
            }

            total = Math.Ceiling(total);
            colonists = Math.Ceiling(colonists);
            colonistApparel = Math.Ceiling(colonistApparel);
            colonistBionics = Math.Ceiling(colonistBionics);
        }
    }

    public class CostCalculator
    {
        private readonly HashSet<string> cheapApparel = new();
        protected HashSet<string> freeApparel = new();

        public CostCalculator()
        {
            cheapApparel.Add("Apparel_Pants");
            cheapApparel.Add("Apparel_BasicShirt");
            cheapApparel.Add("Apparel_Jacket");
        }

        //public void Calculate(CostDetails cost, List<CustomPawn> pawns, List<EquipmentSelection> equipment, List<SelectedAnimal> animals)
        //{
        //    cost.Clear(pawns.Where(pawn => pawn.Type == CustomPawnType.Colonist).Count());

        //    int i = 0;
        //    foreach (var pawn in pawns)
        //    {
        //        if (pawn.Type == CustomPawnType.Colonist)
        //        {
        //            CalculatePawnCost(cost.colonistDetails[i++], pawn);
        //        }
        //    }
        //    foreach (var e in equipment)
        //    {
        //        cost.equipment += CalculateEquipmentCost(e);
        //    }
        //    cost.ComputeTotal();
        //}

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
            //double skillCount = pawn.currentPassions.Keys.Count();
            double passionLevelCount = 0;
            double passionLevelCost = 20;
            //double passionateSkillCount = 0;
            //foreach (SkillDef def in pawn.currentPassions.Keys)
            //{
            //    Passion passion = pawn.currentPassions[def];
            //    int level = pawn.GetSkillLevel(def);

            //    if (passion == Passion.Major)
            //    {
            //        passionLevelCount += 3.0;
            //        passionateSkillCount += 1.0;
            //    }
            //    else if (passion == Passion.Minor)
            //    {
            //        passionLevelCount += 1.0;
            //        passionateSkillCount += 1.0;
            //    }
            //}
            var levelCost = passionLevelCost;
            if (passionLevelCount > 8)
            {
                var penalty = passionLevelCount - 8;
                levelCost += penalty * 0.4;
            }

            cost.marketValue += levelCost * passionLevelCount;

            // Calculate trait cost.
            //if (pawn.TraitCount > Constraints.MaxVanillaTraits)
            //{
            //    int extraTraitCount = pawn.TraitCount - Constraints.MaxVanillaTraits;
            //    double extraTraitCost = 100;
            //    for (int i = 0; i < extraTraitCount; i++)
            //    {
            //        cost.marketValue += extraTraitCost;
            //        extraTraitCost = Math.Ceiling(extraTraitCost * 2.5);
            //    }
            //}

            // Calculate cost of worn apparel.
            //foreach (var layer in PrepareCarefully.Instance.Providers.PawnLayers.GetLayersForPawn(pawn))
            //{
            //    if (layer.Apparel)
            //    {
            //        var def = pawn.GetAcceptedApparel(layer);
            //        if (def == null)
            //        {
            //            continue;
            //        }
            //        EquipmentKey key = new EquipmentKey();
            //        key.ThingDef = def;
            //        key.StuffDef = pawn.GetSelectedStuff(layer);
            //        ThingEntry record = PrepareCarefully.Instance.EquipmentDatabase.Find(key);
            //        if (record == null)
            //        {
            //            continue;
            //        }
            //        EquipmentSelection selection = new EquipmentSelection(record, 1);
            //        double c = CalculateEquipmentCost(selection);
            //        if (def != null)
            //        {
            //            // TODO: Discounted materials should be based on the faction, not hard-coded.
            //            // TODO: Should we continue with the discounting?
            //            if (key.StuffDef != null)
            //            {
            //                if (key.StuffDef.defName == "Synthread")
            //                {
            //                    if (freeApparel.Contains(key.ThingDef.defName))
            //                    {
            //                        c = 0;
            //                    }
            //                    else if (cheapApparel.Contains(key.ThingDef.defName))
            //                    {
            //                        c = c * 0.15d;
            //                    }
            //                }
            //            }
            //        }
            //        cost.apparel += c;
            //    }
            //}

            // Calculate cost for any materials needed for implants.
            //OptionsHealth healthOptions = PrepareCarefully.Instance.Providers.Health.GetOptions(pawn);
            //foreach (Implant option in pawn.Implants)
            //{

            //    // Check if there are any ancestor parts that override the selection.
            //    UniqueBodyPart uniquePart = healthOptions.FindBodyPartsForRecord(option.BodyPartRecord);
            //    if (uniquePart == null)
            //    {
            //        Log.Warning("Prepare Carefully could not find body part record when computing the cost of an implant: " + option.BodyPartRecord.def.defName);
            //        continue;
            //    }
            //    if (pawn.AtLeastOneImplantedPart(uniquePart.Ancestors.Select((UniqueBodyPart p) => { return p.Record; })))
            //    {
            //        continue;
            //    }

            //    //  Figure out the cost of the part replacement based on its recipe's ingredients.
            //    if (option.recipe != null)
            //    {
            //        RecipeDef def = option.recipe;
            //        foreach (IngredientCount amount in def.ingredients)
            //        {
            //            int count = 0;
            //            double totalCost = 0;
            //            bool skip = false;
            //            foreach (ThingDef ingredientDef in amount.filter.AllowedThingDefs)
            //            {
            //                if (ingredientDef.IsMedicine)
            //                {
            //                    skip = true;
            //                    break;
            //                }
            //                count++;
            //                ThingEntry entry = PrepareCarefully.Instance.EquipmentDatabase.LookupThingEntry(new EquipmentKey(ingredientDef, null));
            //                if (entry != null)
            //                {
            //                    totalCost += entry.cost * (double)amount.GetBaseCount();
            //                }
            //            }
            //            if (skip || count == 0)
            //            {
            //                continue;
            //            }
            //            cost.bionics += (int)(totalCost / (double)count);
            //        }
            //    }
            //}

            cost.apparel = Math.Ceiling(cost.apparel);
            cost.bionics = Math.Ceiling(cost.bionics);

            // Use a multiplier to balance pawn cost vs. equipment cost.
            // Disabled for now.
            cost.Multiply(1.0);

            cost.ComputeTotal();
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

        /*
        public double CalculateAnimalCost(SelectedAnimal animal) {
            AnimalRecord record = PrepareCarefully.Instance.AnimalDatabase.FindAnimal(animal.Key);
            if (record != null) {
                return (double)animal.Count * record.Cost;
            }
            else {
                return 0;
            }
        }
        */

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
                Log.Warning("Trying to calculate the cost of a ThingDef with null thingClass: " + def.defName);
                return 0;
            }

            try
            {
                var thing = ThingMaker.MakeThing(def, stuffDef);
                if (thing != null)
                {
                    return thing.MarketValue;
                }

                Log.Warning("Failed when calling MakeThing(" + def.defName +
                            ", ...) to calculate a ThingDef's market value");
                return 0;
            }
            catch (Exception e)
            {
                Log.Warning("Failed to calculate the cost of a ThingDef (" + def.defName + "): ");
                Log.Warning(e.ToString());
                return 0;
            }
        }

        public double CalculateStackCost(ThingDef def, ThingDef stuffDef, double baseCost)
        {
            var cost = baseCost;

            if (def.MadeFromStuff)
            {
                if (def.IsApparel)
                {
                    cost = cost * 1;
                }
                else
                {
                    cost = cost * 0.5;
                }
            }

            if (def.IsRangedWeapon)
            {
                cost = cost * 2;
            }

            //cost = cost * 1.25;
            cost = Math.Round(cost, 1);

            return cost;
        }
    }
}