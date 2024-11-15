namespace SimpleNetAuth.Models.Api;

public class ApiSearchResults<T>
{
    public int TotalCount { get; set; }

    public int TotalPages { get; set; }

    public IList<T> Results { get; set; }
}
