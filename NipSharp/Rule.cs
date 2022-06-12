using System;
using System.Collections.Generic;

namespace NipSharp
{
    public readonly struct Rule
    {
        public Func<IItem, Dictionary<string, float>, Dictionary<string, float>, Dictionary<string, Func<IItem, float>>, Result> Matcher { get; init; }
        public string Line { get; init; }
    }
}
