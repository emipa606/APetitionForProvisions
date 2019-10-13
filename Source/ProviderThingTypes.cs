using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ItemRequests
{
    // This class was adapted from the class ProviderEquipmentTypes
    // from the mod EdB Prepare Carefully by edbmods
    // https://github.com/edbmods/EdBPrepareCarefully/blob/develop/Source/ProviderEquipment.cs
    public class ProviderThingTypes
    {
        protected List<ThingType> types = new List<ThingType>();
        protected Dictionary<ThingType, List<ThingEntry>> equipmentDictionary =
                new Dictionary<ThingType, List<ThingEntry>>();
        protected bool initialized = false;
        public ProviderThingTypes()
        {
            types = ThingDatabase.Instance.ThingTypes.ToList();
        }
        protected void Initialize()
        {
            foreach (var type in types)
            {
                List<ThingEntry> list = ThingDatabase.Instance.AllThingsOfType(type).ToList();
                list.Sort((ThingEntry a, ThingEntry b) => {
                    return a.Label.CompareTo(b.Label);
                });
                equipmentDictionary.Add(type, list);
            }
            initialized = true;
        }
        public bool DatabaseReady
        {
            get
            {
                return ThingDatabase.Instance.Loaded;
            }
        }
        public ThingDatabase.LoadingState LoadingProgress
        {
            get
            {
                return ThingDatabase.Instance.LoadingProgress;
            }
        }
        public IEnumerable<ThingType> Types
        {
            get
            {
                return types;
            }
        }
        public IEnumerable<ThingEntry> AllThingsOfType(ThingType type)
        {
            if (!initialized)
            {
                if (!DatabaseReady)
                {
                    return Enumerable.Empty<ThingEntry>();
                }
                else
                {
                    Initialize();
                }
            }
            List<ThingEntry> result;
            if (equipmentDictionary.TryGetValue(type, out result))
            {
                return result;
            }
            else
            {
                return null;
            }
        }
    }
}
