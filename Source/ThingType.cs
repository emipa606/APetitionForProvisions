using System;
namespace ItemRequests
{
    // This class was adapted from the class EquipmentType
    // from the mod EdB Prepare Carefully by edbmods
    // https://github.com/edbmods/EdBPrepareCarefully/blob/develop/Source/EquipmentType.cs
    public class ThingType
    {
        public ThingType()
        {
        }
        public ThingType(string name)
        {
            Name = Label = name;
        }
        public ThingType(string name, string label)
        {
            Name = name;
            Label = label;
        }
        public string Name
        {
            get;
            set;
        }
        public string Label
        {
            get;
            set;
        }
    }
}
