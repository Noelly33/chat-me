namespace Core.Mensajes.Api.Domain.DTOS
{
    public class MsResponse<T>
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public List<string> Errors { get; set; } = new();
        public T? Data { get; set; }

        public static MsResponse<T> Ok(T data, string message = "Success")
        {
            return new MsResponse<T> { Success = true, Message = message, Data = data };
        }

        public static MsResponse<T> Fail(string error, string message = "An error occurred")
        {
            return new MsResponse<T> { Success = false, Message = message, Errors = new List<string> { error } };
        }

        public static MsResponse<T> Fail(List<string> errors, string message = "Multiple errors occurred")
        {
            return new MsResponse<T> { Success = false, Message = message, Errors = errors };
        }
    }
}
