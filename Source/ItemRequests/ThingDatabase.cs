using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace ItemRequests;

// This class was adapted from the class EquipmentDatabase
// from the mod EdB Prepare Carefully by edbmods
// https://github.com/edbmods/EdBPrepareCarefully/blob/develop/Source/EquipmentDatabase.cs
public class ThingDatabase
{
    private static ThingDatabase instance;

    private readonly HashSet<string> categoryLookup = [];
    private readonly CostCalculator costs = new CostCalculator();
    private readonly Dictionary<ThingKey, ThingEntry> entries = new Dictionary<ThingKey, ThingEntry>();
    private readonly List<ThingDef> stuff = [];
    private readonly HashSet<ThingDef> stuffLookup = [];
    private readonly ThingCategoryDef thingCategoryMeatRaw;

    private readonly ThingCategoryDef thingCategorySweetMeals;

    protected List<ThingEntry> resources = [];

    private ThingDatabase()
    {
        //Log.Message("Initializing ThingDatabase...");
        thingCategorySweetMeals = DefDatabase<ThingCategoryDef>.GetNamedSilentFail("SweetMeals");
        thingCategoryMeatRaw = DefDatabase<ThingCategoryDef>.GetNamedSilentFail("MeatRaw");
    }

    public static ThingDatabase Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new ThingDatabase();
            }

            return instance;
        }
    }

    private LoadingState LoadingProgress { get; } = new LoadingState();
    public bool Loaded => LoadingProgress.phase == LoadingPhase.Loaded;

    public void LoadFrame()
    {
        if (Loaded)
        {
            return;
        }

        if (LoadingProgress.phase == LoadingPhase.NotStarted)
        {
            UpdateLoadingPhase(LoadingPhase.CountingDefs);
        }

        switch (LoadingProgress.phase)
        {
            case LoadingPhase.CountingDefs:
                CountDefs();
                break;
            case LoadingPhase.ProcessingStuff:
                ProcessStuff();
                break;
            case LoadingPhase.ProcessingThings:
                ProcessThings();
                break;
        }
    }

    protected void UpdateLoadingPhase(LoadingPhase phase)
    {
        //Log.Message("UpdateLoadingPhase to " + phase.ToString());
        if (phase != LoadingPhase.Loaded)
        {
            LoadingProgress.enumerator = DefDatabase<ThingDef>.AllDefs.GetEnumerator();
        }

        LoadingProgress.phase = phase;
    }

    private void NextPhase()
    {
        switch (LoadingProgress.phase)
        {
            case LoadingPhase.NotStarted:
                UpdateLoadingPhase(LoadingPhase.CountingDefs);
                break;
            case LoadingPhase.CountingDefs:
                UpdateLoadingPhase(LoadingPhase.ProcessingStuff);
                break;
            case LoadingPhase.ProcessingStuff:
                UpdateLoadingPhase(LoadingPhase.ProcessingThings);
                break;
            case LoadingPhase.ProcessingThings:
                UpdateLoadingPhase(LoadingPhase.Loaded);
                break;
        }
    }

    private void CountDefs()
    {
        for (var i = 0; i < LoadingProgress.defsToCountPerFrame; i++)
        {
            if (LoadingProgress.enumerator.MoveNext())
            {
                continue;
            }

            NextPhase();
            return;
        }
    }

    private void ProcessStuff()
    {
        for (var i = 0; i < LoadingProgress.stuffToProcessPerFrame; i++)
        {
            if (!LoadingProgress.enumerator.MoveNext())
            {
                //Log.Message("Loaded thing database with " + LoadingProgress.stuffCount + " material(s)");
                NextPhase();
                return;
            }

            if (AddStuffToThingLists(LoadingProgress.enumerator.Current))
            {
            }
        }
    }

    private void ProcessThings()
    {
        for (var i = 0; i < LoadingProgress.thingsToProcessPerFrame; i++)
        {
            if (!LoadingProgress.enumerator.MoveNext())
            {
                //Log.Message("Loaded thing database with " + LoadingProgress.thingCount + " item(s)");
                NextPhase();
                return;
            }

            if (AddThingToThingLists(LoadingProgress.enumerator.Current))
            {
            }
        }
    }

    public ThingEntry Find(ThingKey key)
    {
        return entries.GetValueOrDefault(key);
    }

    public void PreloadDefinition(ThingDef def)
    {
        AddStuffToThingLists(def);
        AddThingToThingLists(def);
    }

    private bool AddStuffToThingLists(ThingDef def)
    {
        if (def == null)
        {
            return false;
        }

        if (stuffLookup.Contains(def))
        {
            return false;
        }

        if (def.IsStuff && def.stuffProps != null)
        {
            return AddStuffIfNotThereAlready(def);
        }

        return false;
    }

    private bool AddThingToThingLists(ThingDef def)
    {
        try
        {
            if (def != null)
            {
                var type = ClassifyThingDef(def);
                if (type != ThingType.Other)
                {
                    AddThingDef(def, type);
                    return true;
                }
            }
        }
        catch
        {
            if (def != null)
            {
                Log.Warning($"Failed to process thing definition while building equipment lists: {def.defName}");
            }

            //Log.Message("  Exception: " + e);
        }

        return false;
    }

    private bool FoodTypeIsClassifiedAsFood(ThingDef def)
    {
        var foodTypes = (int)def.ingestible.foodType;
        if ((foodTypes & (int)FoodTypeFlags.Liquor) > 0)
        {
            return true;
        }

        if ((foodTypes & (int)FoodTypeFlags.Meal) > 0)
        {
            return true;
        }

        return (foodTypes & (int)FoodTypeFlags.VegetableOrFruit) > 0;
    }

    private ThingType ClassifyThingDef(ThingDef def)
    {
        if (BelongsToCategoryOrParentCategory(def, ThingCategoryDefOf.Corpses))
        {
            return ThingType.Discard;
        }

        if (BelongsToCategory(def, "Toy"))
        {
            return ThingType.Resources;
        }

        if (def.weaponTags is { Count: > 0 } && def.IsWeapon)
        {
            return ThingType.Weapons;
        }

        if (BelongsToCategoryContaining(def, "Weapon"))
        {
            return ThingType.Weapons;
        }

        if (def.IsApparel && !def.destroyOnDrop)
        {
            return ThingType.Apparel;
        }

        if (BelongsToCategory(def, "Foods"))
        {
            return ThingType.Food;
        }

        // Ingestibles
        if (def.IsDrug || def.statBases != null && def.IsMedicine)
        {
            if (def.ingestible == null)
            {
                return ThingType.Medical;
            }

            if (BelongsToCategory(def, thingCategorySweetMeals))
            {
                return ThingType.Food;
            }

            return FoodTypeIsClassifiedAsFood(def) ? ThingType.Food : ThingType.Medical;
        }

        if (def.ingestible != null)
        {
            if (BelongsToCategory(def, thingCategoryMeatRaw))
            {
                return ThingType.Food;
            }

            if (def.ingestible.drugCategory == DrugCategory.Medical)
            {
                return ThingType.Medical;
            }

            return def.ingestible.preferability == FoodPreferability.DesperateOnly
                ? ThingType.Resources
                : ThingType.Food;
        }

        if (def.CountAsResource)
        {
            // Ammunition should be counted under the weapons category
            if (HasTradeTag(def, "CE_Ammo"))
            {
                return ThingType.Weapons;
            }

            return def.IsShell ? ThingType.Weapons : ThingType.Resources;
        }

        if (def.building != null && def.Minifiable)
        {
            return ThingType.Buildings;
        }

        if (def.race is { Animal: true })
        {
            return ThingType.Animals;
        }

        if (def.category != ThingCategory.Item)
        {
            return ThingType.Other;
        }

        if (def.defName.StartsWith("MechSerum"))
        {
            return ThingType.Medical;
        }

        if (BelongsToCategoryStartingWith(def, "BodyParts"))
        {
            return ThingType.Medical;
        }

        if (BelongsToCategoryContaining(def, "Prostheses"))
        {
            return ThingType.Medical;
        }

        if (BelongsToCategory(def, "GlitterworldParts"))
        {
            return ThingType.Medical;
        }

        return BelongsToCategoryEndingWith(def, "Organs") ? ThingType.Medical : ThingType.Resources;
    }

    // A duplicate of ThingDef.IsWithinCategory(), but with checks to prevent infinite recursion.
    private bool BelongsToCategoryOrParentCategory(ThingDef def, ThingCategoryDef categoryDef)
    {
        if (categoryDef == null || def.thingCategories == null)
        {
            return false;
        }

        categoryLookup.Clear();
        foreach (var thingCategory in def.thingCategories)
        {
            for (var thingCategoryDef = thingCategory;
                 thingCategoryDef != null && !categoryLookup.Contains(thingCategoryDef.defName);
                 thingCategoryDef = thingCategoryDef.parent)
            {
                categoryLookup.Add(thingCategoryDef.defName);
                if (thingCategoryDef.defName == categoryDef.defName)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool BelongsToCategory(ThingDef def, ThingCategoryDef categoryDef)
    {
        if (categoryDef == null || def.thingCategories == null)
        {
            return false;
        }

        return def.thingCategories.FirstOrDefault(d => categoryDef == d) != null;
    }

    private bool BelongsToCategoryStartingWith(ThingDef def, string categoryNamePrefix)
    {
        if (categoryNamePrefix.NullOrEmpty() || def.thingCategories == null)
        {
            return false;
        }

        return def.thingCategories.FirstOrDefault(d => d.defName.StartsWith(categoryNamePrefix)) != null;
    }

    private bool BelongsToCategoryEndingWith(ThingDef def, string categoryNameSuffix)
    {
        if (categoryNameSuffix.NullOrEmpty() || def.thingCategories == null)
        {
            return false;
        }

        return def.thingCategories.FirstOrDefault(d => d.defName.EndsWith(categoryNameSuffix)) != null;
    }

    private bool BelongsToCategoryContaining(ThingDef def, string categoryNameSubstring)
    {
        if (categoryNameSubstring.NullOrEmpty() || def.thingCategories == null)
        {
            return false;
        }

        return def.thingCategories.FirstOrDefault(d => d.defName.Contains(categoryNameSubstring)) != null;
    }

    private bool BelongsToCategory(ThingDef def, string categoryName)
    {
        if (categoryName.NullOrEmpty() || def.thingCategories == null)
        {
            return false;
        }

        return def.thingCategories.FirstOrDefault(d => categoryName == d.defName) != null;
    }

    private bool HasTradeTag(ThingDef def, string tradeTag)
    {
        if (tradeTag.NullOrEmpty() || def.tradeTags == null)
        {
            return false;
        }

        return def.tradeTags.FirstOrDefault(t => tradeTag == t) != null;
    }

    public IEnumerable<ThingEntry> AllThings()
    {
        return entries.Values;
    }

    public IEnumerable<ThingEntry> AllThingsOfType(ThingType type)
    {
        return entries.Values.Where(e => e.type == type);
    }

    public ThingEntry LookupThingEntry(ThingKey key)
    {
        return entries.GetValueOrDefault(key);
    }

    public ThingEntry AddThingDefWithStuff(ThingDef def, ThingDef defStuff, ThingType type)
    {
        var key = new ThingKey(def, defStuff);
        var entry = CreateThingEntry(def, defStuff, type);
        if (entry != null)
        {
            AddRecordIfNotThereAlready(key, entry);
        }

        return entry;
    }

    private void AddRecordIfNotThereAlready(ThingKey key, ThingEntry record)
    {
        if (!entries.TryGetValue(key, out _))
        {
            entries[key] = record;
        }
    }

    private bool AddStuffIfNotThereAlready(ThingDef def)
    {
        if (!stuffLookup.Add(def))
        {
            return false;
        }

        stuff.Add(def);
        return true;
    }

    private void AddThingDef(ThingDef def, ThingType type)
    {
        if (def.MadeFromStuff)
        {
            foreach (var s in stuff)
            {
                if (!s.stuffProps.CanMake(def))
                {
                    continue;
                }

                var key = new ThingKey(def, s);
                var entry = CreateThingEntry(def, s, type);
                if (entry != null)
                {
                    AddRecordIfNotThereAlready(key, entry);
                }
            }
        }
        else if (def.race is { Animal: true })
        {
            if (def.race.hasGenders)
            {
                var femaleEntry = CreateThingEntry(def, Gender.Female, type);
                if (femaleEntry != null)
                {
                    AddRecordIfNotThereAlready(new ThingKey(def, Gender.Female), femaleEntry);
                }

                var maleEntry = CreateThingEntry(def, Gender.Male, type);
                if (maleEntry != null)
                {
                    AddRecordIfNotThereAlready(new ThingKey(def, Gender.Male), maleEntry);
                }
            }
            else
            {
                var key = new ThingKey(def, Gender.None);
                var entry = CreateThingEntry(def, Gender.None, type);
                if (entry != null)
                {
                    AddRecordIfNotThereAlready(key, entry);
                }
            }
        }
        else
        {
            var key = new ThingKey(def);
            var entry = CreateThingEntry(def, null, Gender.None, type);
            if (entry != null)
            {
                AddRecordIfNotThereAlready(key, entry);
            }
        }
    }

    private ThingEntry CreateThingEntry(ThingDef def, ThingDef stuffDef, ThingType type)
    {
        return CreateThingEntry(def, stuffDef, Gender.None, type);
    }

    private ThingEntry CreateThingEntry(ThingDef def, Gender gender, ThingType type)
    {
        return CreateThingEntry(def, null, gender, type);
    }

    private ThingEntry CreateThingEntry(ThingDef def, ThingDef stuffDef, Gender gender, ThingType type)
    {
        var baseCost = costs.GetBaseThingCost(def, stuffDef);
        if (baseCost == 0)
        {
            return null;
        }

        var stackSize = CalculateStackCount();

        var result = new ThingEntry
        {
            type = type,
            def = def,
            stuffDef = stuffDef,
            stackSize = stackSize,
            cost = costs.CalculateStackCost(def, baseCost),
            stacks = true,
            gear = false,
            animal = false
        };
        if (def.MadeFromStuff && stuffDef != null)
        {
            if (stuffDef.stuffProps.allowColorGenerators &&
                (def.colorGenerator != null || def.colorGeneratorInTraderStock != null))
            {
                if (def.colorGenerator != null)
                {
                    result.color = def.colorGenerator.NewRandomizedColor();
                }
                else if (def.colorGeneratorInTraderStock != null)
                {
                    result.color = def.colorGeneratorInTraderStock.NewRandomizedColor();
                }
            }
            else
            {
                result.color = stuffDef.stuffProps.color;
            }
        }
        else
        {
            result.color = def.graphicData?.color ?? Color.white;
        }

        if (def.apparel != null)
        {
            result.stacks = false;
            result.gear = true;
        }

        if (def.weaponTags is { Count: > 0 })
        {
            result.stacks = false;
            result.gear = true;
        }

        if (def.thingCategories != null)
        {
            if (def.thingCategories.SingleOrDefault(d => d.defName == "FoodMeals") != null)
            {
                result.gear = true;
            }

            if (def.thingCategories.SingleOrDefault(d => d.defName == "Medicine") != null)
            {
                result.gear = true;
            }
        }

        if (def.defName == "Apparel_PersonalShield")
        {
            result.hideFromPortrait = true;
        }

        if (def.race is { Animal: true })
        {
            result.animal = true;
            result.gender = gender;
            try
            {
                var pawn = CreatePawn(def, gender);
                if (pawn == null)
                {
                    return null;
                }

                result.thing = pawn;
                result.pawnDef = pawn.kindDef;
            }
            catch
            {
                Log.Warning($"Failed to create a pawn for animal thing entry: {def.defName}");
                //Log.Message("  Exception message: " + e);
                return null;
            }
        }

        result.thing ??= ThingMaker.MakeThing(def, stuffDef);
        return result;
    }


    private int CalculateStackCount()
    {
        return 1;
    }

    private Pawn CreatePawn(ThingDef def, Gender gender)
    {
        var kindDef = (from td in DefDatabase<PawnKindDef>.AllDefs
            where td.race == def
            select td).FirstOrDefault();

        if (kindDef != null)
        {
            var nameGenerator = kindDef.RaceProps.GetNameGenerator(gender);
            if (nameGenerator == null)
            {
                return null;
            }
        }

        if (kindDef == null)
        {
            return null;
        }

        var request = new PawnGenerationRequest(kindDef);
        var pawn = PawnGenerator.GeneratePawn(request);
        if (pawn.Dead || pawn.Downed)
        {
            return null;
        }

        pawn.gender = gender;
        pawn.Drawer.renderer.SetAllGraphicsDirty();
        return pawn;
    }

    private class LoadingState
    {
        public readonly int defsToCountPerFrame = 10;
        public readonly int stuffToProcessPerFrame = 10;
        public readonly int thingsToProcessPerFrame = 10;
        public IEnumerator<ThingDef> enumerator;
        public LoadingPhase phase = LoadingPhase.NotStarted;
    }

    protected enum LoadingPhase
    {
        NotStarted,
        CountingDefs,
        ProcessingStuff,
        ProcessingThings,
        Loaded
    }
}