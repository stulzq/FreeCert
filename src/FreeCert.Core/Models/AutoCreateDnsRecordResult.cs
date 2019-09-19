namespace FreeCert.Core.Models
{
    public class AutoCreateDnsRecordResult
    {
        public AutoCreateDnsRecordResult(bool success, string errorMessage)
        {
            Success = success;
            ErrorMessage = errorMessage;
        }

        public AutoCreateDnsRecordResult(bool success)
        {
            Success = success;
        }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
    }
}