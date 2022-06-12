using System.Collections.Generic;

namespace NipSharp
{
    public interface IItem
    {
        int Type { get; }
        int Name { get; }
        int Class { get; }
        int Color { get; }
        int Quality { get; }
        int Flags { get; }
        int Level { get; }
        IReadOnlyCollection<int> Prefixes { get; }
        IReadOnlyCollection<int> Suffixes { get; }
        IReadOnlyCollection<IStat> Stats { get; }
    }
}
