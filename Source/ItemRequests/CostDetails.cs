using System;
using System.Collections.Generic;
using Verse;

namespace ItemRequests;

public class CostDetails
{
    private readonly List<ColonistCostDetails> colonistDetails = new List<ColonistCostDetails>();

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