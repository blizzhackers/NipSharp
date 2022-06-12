namespace NipSharp
{
    public struct Result
    {
        public Outcome Outcome { get; set; }
        public string Line { get; set; }
        public float MaxQuantity { get; set; }
        public float Tier { get; set; }
        public float MercTier { get; set; }
        public float CharmTier { get; set; }
        public float SwapTier { get; set; }
        public override string ToString()
        {
            return $"{nameof(Outcome)}: {Outcome},\n" +
                   $"{nameof(Line)}: {Line},\n" +
                   $"{nameof(MaxQuantity)}:{MaxQuantity},\n" +
                   $"{nameof(Tier)}: {Tier},\n" +
                   $"{nameof(MercTier)}: {MercTier},\n" +
                   $"{nameof(CharmTier)}: {CharmTier},\n" +
                   $"{nameof(SwapTier)}: {SwapTier}";
        }
    }
}
