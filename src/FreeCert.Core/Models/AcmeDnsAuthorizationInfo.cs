namespace FreeCert.Core.Models
{
    public class AcmeDnsAuthorizationInfo
    {
        public string Record { get; set; }
        public string RecordType { get; set; }
        public string Value { get; set; }
        public string Status { get; set; }
    }
}