namespace NipSharp.Test
{
    internal class FakeStat : IStat
    {
        public int Id { get; init; }
        public int Layer { get; init; }
        public int Value { get; init; }
    }
}