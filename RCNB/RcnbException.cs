using System;

namespace RCNB
{

    //[System.Serializable]
    public class RcnbException : Exception
    {
        public RcnbException() { }
        public RcnbException(string message) : base(message) { }
        public RcnbException(string message, Exception inner) : base(message, inner) { }
        //protected RcnbException(
        //  System.Runtime.Serialization.SerializationInfo info,
        //  System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
