using System;
using System.Collections.Generic;

namespace NipSharp
{
    public readonly struct Rule
    {
        public Func<Dictionary<string, float>, Outcome> Matcher { get; init; }
        public string Line { get; init; }
    }
}
