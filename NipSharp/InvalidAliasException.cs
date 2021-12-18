using System;

namespace NipSharp
{
    public class InvalidAliasException : Exception
    {
        public InvalidAliasException(string message) : base(message)
        {
        }
    }
}
