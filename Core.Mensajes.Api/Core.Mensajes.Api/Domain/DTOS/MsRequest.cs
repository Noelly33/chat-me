namespace Core.Mensajes.Api.Domain.DTOS
{
    public class MsRequest<T>
    {
        public RequestHeader Header { get; set; } = new();
        public T Data { get; set; } = default!;
    }
    public class RequestHeader
    {
        public string TransactionId { get; set; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string? Device { get; set; }
    }
}
