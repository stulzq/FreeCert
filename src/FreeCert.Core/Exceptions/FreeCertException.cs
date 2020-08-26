using System;

namespace FreeCert.Core.Exceptions
{
    public class FreeCertException:Exception
    {
        public FreeCertException(string message):base(message)
        {
            
        }

        public FreeCertException(string message,Exception inner) : base(message,inner)
        {

        }
    }
}