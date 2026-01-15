using System.Runtime.Serialization;

namespace Primary.RHI2
{
    public class RHIException : Exception
    {
        public RHIException()
        {
        }

        public RHIException(string? message) : base(message)
        {
        }

        public RHIException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        protected RHIException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
