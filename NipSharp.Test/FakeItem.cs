using System;
using System.Collections.Generic;

namespace NipSharp.Test
{
    internal class FakeItem : IItem
    {
        public int Type { get; init; }
        public int Name { get; init; }
        public int Class { get; init; }
        public int Color { get; init; }
        public int Quality { get; init; }
        public int Flags { get; init; }
        public int Level { get; init; }
        public IReadOnlyCollection<int> Prefixes { get; init; } = Array.Empty<int>();
        public IReadOnlyCollection<int> Suffixes { get; init; } = Array.Empty<int>();
        public IReadOnlyCollection<IStat> Stats { get; init; } = Array.Empty<IStat>();
    }
}