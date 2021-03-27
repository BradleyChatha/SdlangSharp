using System;
using System.Collections.Generic;
using System.Text;

namespace SdlangSharp
{
    [Serializable]
    public class SdlException : Exception
    {
        public SdlException() { }
        public SdlException(string message) : base(message) { }
        public SdlException(string message, Exception inner) : base(message, inner) { }
        protected SdlException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
