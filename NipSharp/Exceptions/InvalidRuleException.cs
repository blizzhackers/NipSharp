using System;

namespace NipSharp.Exceptions
{
    public class InvalidRuleException : NipException
    {
        public InvalidRuleException(string message, Exception cause) : base(message, cause)
        {
        }
    }
}
