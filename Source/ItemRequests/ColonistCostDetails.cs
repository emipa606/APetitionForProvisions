using System;

namespace ItemRequests
{
    public class ColonistCostDetails
    {
        public double apparel;

        public double bionics;

        public double marketValue;

        public string name;

        public double passionCount = 0;

        public double total;

        private double animals;

        private double passions;

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
}