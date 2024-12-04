namespace SimpleAuthNet.Models.SearchOptions;

public class AppRoleSearchOptions : ISearchOptions
{
    public int? Id { get; set; }

    public string? Name { get; set; }

    public string? SortField { get; set; }

    public string? SortDirection { get; set; }
}