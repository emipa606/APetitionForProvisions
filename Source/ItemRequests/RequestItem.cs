using Verse;

namespace ItemRequests;

public class RequestItem : IExposable
{
    public int amount;

    public bool isPawn;

    public ThingEntry item;

    public float pricePerItem;

    public bool removed;

    public void ExposeData()
    {
        Scribe_Deep.Look(ref item, "item");
        Scribe_Values.Look(ref amount, "amount");
        Scribe_Values.Look(ref pricePerItem, "pricePerItem");
        Scribe_Values.Look(ref isPawn, "isPawn");
        Scribe_Values.Look(ref removed, "removed");
    }
}