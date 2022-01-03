using System;

namespace NipSharp.Exceptions
{
    public class InvalidAliasException : NipException
    {
        public InvalidAliasException(string message) : base(message)
        {
        }
    }
}