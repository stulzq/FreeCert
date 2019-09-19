using System;
using System.Collections.Generic;

namespace FreeCert.Core.Models
{
    public class AcmeOrderInfo
    {
        public DateTime? Expires { get; set; }

        public List<string> Domains { get; set; }
        public string Status { get; set; }
    }
}