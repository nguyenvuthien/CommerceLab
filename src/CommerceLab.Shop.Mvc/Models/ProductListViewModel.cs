namespace CommerceLab.Shop.Mvc.Models;

public class ProductListViewModel
{
    public List<ProductListItemViewModel> Items { get; set; } = new();

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 2;

    public int TotalCount { get; set; }

    public int TotalPages =>
        (int)Math.Ceiling((double)TotalCount / PageSize);

    public bool HasPreviousPage => Page > 1;

    public bool HasNextPage => Page < TotalPages;
    public string Sort { get; set; } = "id";

}