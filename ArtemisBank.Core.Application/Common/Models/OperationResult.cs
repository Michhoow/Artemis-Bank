namespace ArtemisBank.Core.Application.Common.Models
{
    public class OperationResult
    {
        public bool Succeeded { get; set; }
        public string? ErrorMessage { get; set; }
        public string? Warning { get; set; }
        public string? Reference { get; set; }

        public static OperationResult Success(string? reference = null, string? warning = null)
            => new OperationResult { Succeeded = true, Reference = reference, Warning = warning };

        public static OperationResult Failure(string message)
            => new OperationResult { Succeeded = false, ErrorMessage = message };
    }
}
