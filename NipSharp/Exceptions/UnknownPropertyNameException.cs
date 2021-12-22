using System;

namespace NipSharp.Exceptions
{
    public class UnknownPropertyNameException : NipException
    {
        public UnknownPropertyNameException(string message) : base(message)
        {
        }
    }
}
