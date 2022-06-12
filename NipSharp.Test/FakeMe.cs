using System;
using System.Collections.Generic;

namespace NipSharp.Test
{
    internal class FakeMe : IMe
    {
        public int Act { get; set; }
        public int Level { get; set; }
        public int Difficulty { get; set; }
        public int Gold { get; set; }
    }
}
