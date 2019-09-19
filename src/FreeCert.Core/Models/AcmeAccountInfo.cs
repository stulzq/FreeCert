using System;
using System.Collections.Generic;

namespace FreeCert.Core.Models
{
    public class AcmeAccountInfo
    {
        public string Status { get; set; }
        public List<string> Contacts { get; set; }
        public bool? AcceptTos { get; set; }
    }
}