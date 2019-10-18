using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using UnityEngine;
using Verse;

namespace ItemRequests
{
    // This class was adapted from the class EquipmentDatabase
    // from the mod EdB Prepare Carefully by edbmods
    // https://github.com/edbmods/EdBPrepareCarefully/blob/develop/Source/EquipmentDatabase.cs
    public class ThingDatabase
    {
        protected Dictionary<ThingKey, ThingEntry> entries = new Dictionary<ThingKey, ThingEntry>();

        protected List<ThingEntry> resources = new List<ThingEntry>();
        protected List<ThingDef> stuff = new List<ThingDef>();
        protected HashSet<ThingDef> stuffLookup = new HashSet<ThingDef>();
        protected CostCalculator costs = new CostCalculator();
        //protected List<ThingType> types = new List<ThingType>();

        protected ThingCategoryDef thingCategorySweetMeals = null;
        protected ThingCategoryDef thingCategoryMeatRaw = null;
        private static ThingDatabase instance;

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

        private ThingDatabase()
        {
            Log.Message("Initializing ThingDatabase...");
            
            thingCategorySweetMeals = DefDatabase<ThingCategoryDef>.GetNamedSilentFail("SweetMeals");
            thingCategoryMeatRaw = DefDatabase<ThingCategoryDef>.GetNamedSilentFail("MeatRaw");
        }

        public LoadingState LoadingProgress { get; protected set; } = new LoadingState();
        public bool Loaded
        {
            get { return LoadingProgress.phase == LoadingPhase.Loaded; }
        }

        public class LoadingState
        {
            public LoadingPhase phase = LoadingPhase.NotStarted;
            public IEnumerator<ThingDef> enumerator;
            public int thingsProcessed = 0;
            public int stuffProcessed = 0;
            public int defsToCountPerFrame = 500;
            public int stuffToProcessPerFrame = 100;
            public int thingsToProcessPerFrame = 50;
            public int defCount = 0;
            public int stuffCount = 0;
            public int thingCount = 0;
        }

        public enum LoadingPhase
        {
            NotStarted,
            CountingDefs,
            ProcessingStuff,
            ProcessingThings,
            Loaded
        }

        public void LoadFrame()
        {
            if (Loaded)
            {
                return;
            }
            else if (LoadingProgress.phase == LoadingPhase.NotStarted)
            {
                UpdateLoadingPhase(LoadingPhase.CountingDefs);
            }

            if (LoadingProgress.phase == LoadingPhase.CountingDefs)
            {
                CountDefs();
            }
            else if (LoadingProgress.phase == LoadingPhase.ProcessingStuff)
            {
                ProcessStuff();
            }
            else if (LoadingProgress.phase == LoadingPhase.ProcessingThings)
            {
                ProcessThings();
            }
        }

        protected void UpdateLoadingPhase(LoadingPhase phase)
        {
            if (phase != LoadingPhase.Loaded)
            {
                LoadingProgress.enumerator = DefDatabase<ThingDef>.AllDefs.GetEnumerator();
            }
            LoadingProgress.phase = phase;
        }

        protected void NextPhase()
        {
            if (LoadingProgress.phase == LoadingPhase.NotStarted)
            {
                UpdateLoadingPhase(LoadingPhase.CountingDefs);
            }
            else if (LoadingProgress.phase == LoadingPhase.CountingDefs)
            {
                UpdateLoadingPhase(LoadingPhase.ProcessingStuff);
            }
            else if (LoadingProgress.phase == LoadingPhase.ProcessingStuff)
            {
                UpdateLoadingPhase(LoadingPhase.ProcessingThings);
            }
            else if (LoadingProgress.phase == LoadingPhase.ProcessingThings)
            {
                UpdateLoadingPhase(LoadingPhase.Loaded);
            }
        }

        protected void CountDefs()
        {
            for (int i = 0; i < LoadingProgress.defsToCountPerFrame; i++)
            {
                if (!LoadingProgress.enumerator.MoveNext())
                {
                    NextPhase();
                    return;
                }
                LoadingProgress.defCount++;
            }
        }

        protected void ProcessStuff()
        {
            for (int i = 0; i < LoadingProgress.stuffToProcessPerFrame; i++)
            {
                if (!LoadingProgress.enumerator.MoveNext())
                {
                    Log.Message("Loaded thing database with " + LoadingProgress.stuffCount + " material(s)");
                    NextPhase();
                    return;
                }
                if (AddStuffToThingLists(LoadingProgress.enumerator.Current))
                {
                    LoadingProgress.stuffCount++;
                }
                LoadingProgress.stuffProcessed++;
            }
        }

        protected void ProcessThings()
        {
            for (int i = 0; i < LoadingProgress.thingsToProcessPerFrame; i++)
            {
                if (!LoadingProgress.enumerator.MoveNext())
                {
                    Log.Message("Loaded thing database with " + LoadingProgress.thingCount + " item(s)");
                    NextPhase();
                    return;
                }
                if (AddThingToThingLists(LoadingProgress.enumerator.Current))
                {
                    LoadingProgress.thingCount++;
                }
                LoadingProgress.thingsProcessed++;
            }
        }

        public ThingEntry Find(ThingKey key)
        {
            ThingEntry result;
            if (entries.TryGetValue(key, out result))
            {
                return result;
            }
            else
            {
                return null;
            }
        }
        
        public void PreloadDefinition(ThingDef def)
        {
            AddStuffToThingLists(def);
            AddThingToThingLists(def);
        }

        protected bool AddStuffToThingLists(ThingDef def)
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
            else
            {
                return false;
            }
        }

        protected bool AddThingToThingLists(ThingDef def)
        {
            try
            {
                if (def != null)
                {
                    ThingType type = ClassifyThingDef(def);
                    if (type != ThingType.Other)
                    {
                        AddThingDef(def, type);
                        return true;
                    }
                }
            }
            catch (Exception e)
            {
                Log.Warning("Failed to process thing definition while building equipment lists: " + def.defName);
                Log.Message("  Exception: " + e);
            }
            return false;
        }

        private bool FoodTypeIsClassifiedAsFood(ThingDef def)
        {
            int foodTypes = (int)def.ingestible.foodType;
            if ((foodTypes & (int)FoodTypeFlags.Liquor) > 0)
            {
                return true;
            }
            if ((foodTypes & (int)FoodTypeFlags.Meal) > 0)
            {
                return true;
            }
            if ((foodTypes & (int)FoodTypeFlags.VegetableOrFruit) > 0)
            {
                return true;
            }
            return false;
        }

        public ThingType ClassifyThingDef(ThingDef def)
        {

            if (BelongsToCategory(def, "Toy"))
            {
                return ThingType.Resources;
            }
            if (def.weaponTags != null && def.weaponTags.Count > 0 && def.IsWeapon)
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
            if (def.IsDrug || (def.statBases != null && def.IsMedicine))
            {
                if (def.ingestible != null)
                {
                    if (BelongsToCategory(def, thingCategorySweetMeals))
                    {
                        return ThingType.Food;
                    }
                    if (FoodTypeIsClassifiedAsFood(def))
                    {
                        return ThingType.Food;
                    }
                }
                return ThingType.Medical;
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
                if (def.ingestible.preferability == FoodPreferability.DesperateOnly)
                {
                    return ThingType.Resources;
                }
                return ThingType.Food;
            }

            if (def.CountAsResource)
            {
                // Ammunition should be counted under the weapons category
                if (HasTradeTag(def, "CE_Ammo"))
                {
                    return ThingType.Weapons;
                }
                if (def.IsShell)
                {
                    return ThingType.Weapons;
                }

                return ThingType.Resources;
            }

            if (def.building != null && def.Minifiable)
            {
                return ThingType.Buildings;
            }

            if (def.race != null && def.race.Animal == true)
            {
                return ThingType.Animals;
            }

            if (def.category == ThingCategory.Item)
            {
                if (def.defName.StartsWith("MechSerum"))
                {
                    return ThingType.Medical;
                }
                // Body parts should be medical
                if (BelongsToCategoryStartingWith(def, "BodyParts"))
                {
                    return ThingType.Medical;
                }
                // EPOE parts should be medical
                if (BelongsToCategoryContaining(def, "Prostheses"))
                {
                    return ThingType.Medical;
                }
                if (BelongsToCategory(def, "GlitterworldParts"))
                {
                    return ThingType.Medical;
                }
                if (BelongsToCategoryEndingWith(def, "Organs"))
                {
                    return ThingType.Medical;
                }
                return ThingType.Resources;
            }

            return ThingType.Other;
        }

        private HashSet<string> categoryLookup = new HashSet<string>();
        // A duplicate of ThingDef.IsWithinCategory(), but with checks to prevent infinite recursion.
        public bool BelongsToCategoryOrParentCategory(ThingDef def, ThingCategoryDef categoryDef)
        {
            if (categoryDef == null || def.thingCategories == null)
            {
                return false;
            }
            categoryLookup.Clear();
            for (int i = 0; i < def.thingCategories.Count; i++)
            {
                for (ThingCategoryDef thingCategoryDef = def.thingCategories[i]; thingCategoryDef != null && !categoryLookup.Contains(thingCategoryDef.defName); thingCategoryDef = thingCategoryDef.parent)
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

        public bool BelongsToCategory(ThingDef def, ThingCategoryDef categoryDef)
        {
            if (categoryDef == null || def.thingCategories == null)
            {
                return false;
            }
            return def.thingCategories.FirstOrDefault(d => {
                return categoryDef == d;
            }) != null;
        }

        public bool BelongsToCategoryStartingWith(ThingDef def, string categoryNamePrefix)
        {
            if (categoryNamePrefix.NullOrEmpty() || def.thingCategories == null)
            {
                return false;
            }
            return def.thingCategories.FirstOrDefault(d => {
                return d.defName.StartsWith(categoryNamePrefix);
            }) != null;
        }

        public bool BelongsToCategoryEndingWith(ThingDef def, string categoryNameSuffix)
        {
            if (categoryNameSuffix.NullOrEmpty() || def.thingCategories == null)
            {
                return false;
            }
            return def.thingCategories.FirstOrDefault(d => {
                return d.defName.EndsWith(categoryNameSuffix);
            }) != null;
        }

        public bool BelongsToCategoryContaining(ThingDef def, string categoryNameSubstring)
        {
            if (categoryNameSubstring.NullOrEmpty() || def.thingCategories == null)
            {
                return false;
            }
            return def.thingCategories.FirstOrDefault(d => {
                return d.defName.Contains(categoryNameSubstring);
            }) != null;
        }

        public bool BelongsToCategory(ThingDef def, string categoryName)
        {
            if (categoryName.NullOrEmpty() || def.thingCategories == null)
            {
                return false;
            }
            return def.thingCategories.FirstOrDefault(d => {
                return categoryName == d.defName;
            }) != null;
        }

        public bool HasTradeTag(ThingDef def, string tradeTag)
        {
            if (tradeTag.NullOrEmpty() || def.tradeTags == null)
            {
                return false;
            }
            return def.tradeTags.FirstOrDefault(t => {
                return tradeTag == t;
            }) != null;
        }

        public IEnumerable<ThingEntry> AllThingsOfType(ThingType type)
        {
            return entries.Values.Where((ThingEntry e) => {
                return e.type == type;
            });
        }

        public IEnumerable<ThingEntry> AllThings()
        {
            return entries.Values;
        }

        public List<ThingEntry> Resources
        {
            get
            {
                List<ThingEntry> result = entries.Values.ToList().FindAll((ThingEntry e) => {
                    return e.type == ThingType.Resources;
                });
                result.Sort((ThingEntry a, ThingEntry b) => {
                    return a.Label.CompareTo(b.Label);
                });
                return result;
            }
        }

        public List<ThingEntry> Food
        {
            get
            {
                List<ThingEntry> result = entries.Values.ToList().FindAll((ThingEntry e) => {
                    return e.type == ThingType.Food;
                });
                result.Sort((ThingEntry a, ThingEntry b) => {
                    return a.Label.CompareTo(b.Label);
                });
                return result;
            }
        }

        public List<ThingEntry> Weapons
        {
            get
            {
                List<ThingEntry> result = entries.Values.ToList().FindAll((ThingEntry e) => {
                    return e.type == ThingType.Weapons;
                });
                result.Sort((ThingEntry a, ThingEntry b) => {
                    return a.Label.CompareTo(b.Label);
                });
                return result;
            }
        }

        public List<ThingEntry> Apparel
        {
            get
            {
                List<ThingEntry> result = entries.Values.ToList().FindAll((ThingEntry e) => {
                    return e.type == ThingType.Apparel;
                });
                result.Sort((ThingEntry a, ThingEntry b) => {
                    return a.Label.CompareTo(b.Label);
                });
                return result;
            }
        }

        public List<ThingEntry> Animals
        {
            get
            {
                List<ThingEntry> result = entries.Values.ToList().FindAll((ThingEntry e) => {
                    return e.type == ThingType.Animals;
                });
                result.Sort((ThingEntry a, ThingEntry b) => {
                    return a.Label.CompareTo(b.Label);
                });
                return result;
            }
        }

        public List<ThingEntry> Implants
        {
            get
            {
                List<ThingEntry> result = entries.Values.ToList().FindAll((ThingEntry e) => {
                    return e.type == ThingType.Medical;
                });
                result.Sort((ThingEntry a, ThingEntry b) => {
                    return a.Label.CompareTo(b.Label);
                });
                return result;
            }
        }

        public List<ThingEntry> Buildings
        {
            get
            {
                List<ThingEntry> result = entries.Values.ToList().FindAll((ThingEntry e) => {
                    return e.type == ThingType.Buildings;
                });
                result.Sort((ThingEntry a, ThingEntry b) => {
                    return a.Label.CompareTo(b.Label);
                });
                return result;
            }
        }

        public List<ThingEntry> Other
        {
            get
            {
                List<ThingEntry> result = entries.Values.ToList().FindAll((ThingEntry e) => {
                    return e.type == ThingType.Other;
                });
                result.Sort((ThingEntry a, ThingEntry b) => {
                    return a.Label.CompareTo(b.Label);
                });
                return result;
            }
        }

        public ThingEntry LookupThingEntry(ThingKey key)
        {
            ThingEntry result;
            if (entries.TryGetValue(key, out result))
            {
                return result;
            }
            else
            {
                return null;
            }
        }

        public ThingEntry AddThingDefWithStuff(ThingDef def, ThingDef stuff, ThingType type)
        {
            ThingKey key = new ThingKey(def, stuff);
            ThingEntry entry = CreateThingEntry(def, stuff, type);
            if (entry != null)
            {
                AddRecordIfNotThereAlready(key, entry);
            }
            return entry;
        }

        protected bool AddRecordIfNotThereAlready(ThingKey key, ThingEntry record)
        {
            if (entries.TryGetValue(key, out ThingEntry value))
            {
                return false;
            }
            else
            {
                entries[key] = record;
                return true;
            }
        }

        protected bool AddStuffIfNotThereAlready(ThingDef def)
        {
            if (stuffLookup.Contains(def))
            {
                return false;
            }
            stuffLookup.Add(def);
            stuff.Add(def);
            return true;
        }

        protected void AddThingDef(ThingDef def, ThingType type)
        {
            if (def.MadeFromStuff)
            {
                foreach (var s in stuff)
                {
                    if (s.stuffProps.CanMake(def))
                    {
                        ThingKey key = new ThingKey(def, s);
                        ThingEntry entry = CreateThingEntry(def, s, type);
                        if (entry != null)
                        {
                            AddRecordIfNotThereAlready(key, entry);
                        }
                    }
                }
            }
            else if (def.race != null && def.race.Animal)
            {
                if (def.race.hasGenders)
                {
                    ThingEntry femaleEntry = CreateThingEntry(def, Gender.Female, type);
                    if (femaleEntry != null)
                    {
                        AddRecordIfNotThereAlready(new ThingKey(def, Gender.Female), femaleEntry);
                    }
                    ThingEntry maleEntry = CreateThingEntry(def, Gender.Male, type);
                    if (maleEntry != null)
                    {
                        AddRecordIfNotThereAlready(new ThingKey(def, Gender.Male), maleEntry);
                    }
                }
                else
                {
                    ThingKey key = new ThingKey(def, Gender.None);
                    ThingEntry entry = CreateThingEntry(def, Gender.None, type);
                    if (entry != null)
                    {
                        AddRecordIfNotThereAlready(key, entry);
                    }
                }
            }
            else
            {
                ThingKey key = new ThingKey(def, null);
                ThingEntry entry = CreateThingEntry(def, null, Gender.None, type);
                if (entry != null)
                {
                    AddRecordIfNotThereAlready(key, entry);
                }
            }
        }

        protected ThingEntry CreateThingEntry(ThingDef def, ThingDef stuffDef, ThingType type)
        {
            return CreateThingEntry(def, stuffDef, Gender.None, type);
        }

        protected ThingEntry CreateThingEntry(ThingDef def, Gender gender, ThingType type)
        {
            return CreateThingEntry(def, null, gender, type);
        }

        protected ThingEntry CreateThingEntry(ThingDef def, ThingDef stuffDef, Gender gender, ThingType type)
        {
            double baseCost = costs.GetBaseThingCost(def, stuffDef);
            if (baseCost == 0)
            {
                return null;
            }
            int stackSize = CalculateStackCount(def, baseCost);

            ThingEntry result = new ThingEntry();
            result.type = type;
            result.def = def;
            result.stuffDef = stuffDef;
            result.stackSize = stackSize;
            result.cost = costs.CalculateStackCost(def, stuffDef, baseCost);
            result.stacks = true;
            result.gear = false;
            result.animal = false;
            if (def.MadeFromStuff && stuffDef != null)
            {
                if (stuffDef.stuffProps.allowColorGenerators && (def.colorGenerator != null || def.colorGeneratorInTraderStock != null))
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
                if (def.graphicData != null)
                {
                    result.color = def.graphicData.color;
                }
                else
                {
                    result.color = Color.white;
                }
            }
            if (def.apparel != null)
            {
                result.stacks = false;
                result.gear = true;
            }
            if (def.weaponTags != null && def.weaponTags.Count > 0)
            {
                result.stacks = false;
                result.gear = true;
            }

            if (def.thingCategories != null)
            {
                if (def.thingCategories.SingleOrDefault((ThingCategoryDef d) => {
                    return (d.defName == "FoodMeals");
                }) != null)
                {
                    result.gear = true;
                }
                if (def.thingCategories.SingleOrDefault((ThingCategoryDef d) => {
                    return (d.defName == "Medicine");
                }) != null)
                {
                    result.gear = true;
                }
            }

            if (def.defName == "Apparel_PersonalShield")
            {
                result.hideFromPortrait = true;
            }

            if (def.race != null && def.race.Animal)
            {
                result.animal = true;
                result.gender = gender;
                try
                {
                    Pawn pawn = CreatePawn(def, stuffDef, gender);
                    if (pawn == null)
                    {
                        return null;
                    }
                    else
                    {
                        result.thing = pawn;
                    }
                }
                catch (Exception e)
                {
                    Log.Warning("Failed to create a pawn for animal equipment entry: " + def.defName);
                    Log.Message("  Exception message: " + e);
                    return null;
                }
            }

            if (result.thing == null)
            {
                result.thing = ThingMaker.MakeThing(def, stuffDef);
            }
            return result;
        }


        public int CalculateStackCount(ThingDef def, double basePrice)
        {
            return 1;
        }

        public Pawn CreatePawn(ThingDef def, ThingDef stuffDef, Gender gender)
        {
            PawnKindDef kindDef = (from td in DefDatabase<PawnKindDef>.AllDefs
                                   where td.race == def
                                   select td).FirstOrDefault();

            RulePackDef nameGenerator = kindDef.RaceProps.GetNameGenerator(gender);
            if (nameGenerator == null)
            {
                return null;
            }

            if (kindDef != null)
            {
                Faction faction = Faction.OfPlayer;
                PawnGenerationRequest request = new PawnGenerationRequest(kindDef, faction, PawnGenerationContext.NonPlayer,
                    -1, false, false, true, true, true, false, 1f, false, true, true, false, false, false,
                    false, null, null, null, null, null, null, null, null);
                Pawn pawn = PawnGenerator.GeneratePawn(request);
                if (pawn.Dead || pawn.Downed)
                {
                    return null;
                }
                pawn.gender = gender;
                pawn.Drawer.renderer.graphics.ResolveAllGraphics();
                return pawn;
            }
            else
            {
                return null;
            }
        }
    }
}
