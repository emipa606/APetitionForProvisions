using Verse;

namespace ItemRequests;

public readonly struct ThingKey(ThingDef thingDef, ThingDef stuffDef = null, Gender gender = Gender.None)
{
    public ThingDef ThingDef { get; } = thingDef;

    public ThingDef StuffDef { get; } = stuffDef;

    public Gender Gender { get; } = gender;

    public ThingKey(ThingDef thingDef, Gender gender) : this(thingDef, null, gender)
    {
    }

    public override bool Equals(object o)
    {
        if (o is not ThingKey pair)
        {
            return false;
        }

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