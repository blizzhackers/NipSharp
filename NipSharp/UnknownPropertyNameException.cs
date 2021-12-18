using System;

namespace NipSharp
{
    public class UnknownPropertyNameException : Exception
    {
        public UnknownPropertyNameException(string message) : base(message)
        {
        }
    }
}
