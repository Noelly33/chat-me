namespace Core.Mensajes.Api.Domain.Entities
{
    public class PagedResponse<T>
    {
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public int TotalRecords { get; set; }
        public IEnumerable<T> Data { get; set; }

        public PagedResponse(IEnumerable<T> data, int pageNumber, int pageSize, int totalRecords)
        {
            PageNumber = pageNumber;
            PageSize = pageSize;
            TotalRecords = totalRecords;
            TotalPages = (int)Math.Ceiling((double)totalRecords / pageSize);
            Data = data;
        }
        public PagedResponse()
        {
            Data = Enumerable.Empty<T>();
        }
    }
}
