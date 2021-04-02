using Verse;

namespace ItemRequests
{
    public readonly struct ThingKey
    {
        public ThingDef ThingDef { get; }

        public ThingDef StuffDef { get; }

        public Gender Gender { get; }

        public ThingKey(ThingDef thingDef, ThingDef stuffDef, Gender gender)
        {
            ThingDef = thingDef;
            StuffDef = stuffDef;
            Gender = gender;
        }

        public ThingKey(ThingDef thingDef, ThingDef stuffDef)
        {
            ThingDef = thingDef;
            StuffDef = stuffDef;
            Gender = Gender.None;
        }

        public ThingKey(ThingDef thingDef)
        {
            ThingDef = thingDef;
            StuffDef = null;
            Gender = Gender.None;
        }

        public ThingKey(ThingDef thingDef, Gender gender)
        {
            ThingDef = thingDef;
            StuffDef = null;
            Gender = gender;
        }

        public override bool Equals(object o)
        {
            if (o == null)
            {
                return false;
            }

            if (!(o is ThingKey))
            {
                return false;
            }

            var pair = (ThingKey) o;
            return ThingDef == pair.ThingDef && StuffDef == pair.StuffDef;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var a = ThingDef != null ? ThingDef.GetHashCode() : 0;
                var b = StuffDef != null ? StuffDef.GetHashCode() : 0;
                return (((31 * a) + b) * 31) + Gender.GetHashCode();
            }
        }

        public override string ToString()
        {
            return
                $"[ThingKey: def = {(ThingDef != null ? ThingDef.defName : "null")}, stuffDef = {(StuffDef != null ? StuffDef.defName : "null")}, gender = {Gender}]";
        }
    }
}