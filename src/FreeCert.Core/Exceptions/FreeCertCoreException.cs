using System;

namespace FreeCert.Core.Exceptions
{
    public class FreeCertCoreException:Exception
    {
        public FreeCertCoreException(string message):base(message)
        {
            
        }

        public FreeCertCoreException(string message,Exception inner) : base(message,inner)
        {

        }
    }
}