namespace SimpleAuthNet.Models.SearchOptions;

public class AppUserSearchOptions : ISearchOptions
{
    public int? Id { get; set; }

    public string? Username { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public DateTime? DateEnteredLower { get; set; }

    public DateTime? DateEnteredUpper { get; set; }

    public DateTime? LastSeenLower { get; set; }

    public DateTime? LastSeenUpper { get; set; }
    public string? SortField { get; set; }

    public string? SortDirection { get; set; }
}
