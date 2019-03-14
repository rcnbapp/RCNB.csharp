using System;

namespace RCNB
{

    //[System.Serializable]
    public class RcnbOverflowException : RcnbException
    {
        public RcnbOverflowException() { }
        public RcnbOverflowException(string message) : base(message) { }
        public RcnbOverflowException(string message, Exception inner) : base(message, inner) { }
        //protected RcnbOverflowException(
        //  System.Runtime.Serialization.SerializationInfo info,
        //  System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
