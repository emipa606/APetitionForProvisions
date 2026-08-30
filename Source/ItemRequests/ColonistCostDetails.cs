using System;

namespace ItemRequests;

public class ColonistCostDetails
{
    private double animals;

    public string name;

    private double passions;

    private double traits;
    public double Apparel { get; set; }

    public double Bionics { get; set; }

    public double MarketValue { get; set; }

    public double PassionCount { get; } = 0;

    public double Total { get; private set; }

    public void Clear()
    {
        Total = 0;
        passions = 0;
        traits = 0;
        Apparel = 0;
        Bionics = 0;
        animals = 0;
        MarketValue = 0;
    }

    public void ComputeTotal()
    {
        Total = Math.Ceiling(passions + traits + Apparel + Bionics + MarketValue + animals);
    }

    public void Multiply(double amount)
    {
        passions = Math.Ceiling(passions * amount);
        traits = Math.Ceiling(traits * amount);
        MarketValue = Math.Ceiling(MarketValue * amount);
        ComputeTotal();
    }
}