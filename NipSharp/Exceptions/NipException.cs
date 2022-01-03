using System;

namespace NipSharp.Exceptions
{
    public abstract class NipException : Exception
    {
        public NipException(string message) : base(message)
        {
        }

        public NipException(string message, Exception cause) : base(message, cause)
        {
        }
    }
}
