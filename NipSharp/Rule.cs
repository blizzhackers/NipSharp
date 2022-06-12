using System;
using System.Collections.Generic;

namespace NipSharp
{
    public readonly struct Rule
    {
        public Func<Dictionary<string, float>, Dictionary<string, float>, Result> Matcher { get; init; }
        public string Line { get; init; }
    }
}
