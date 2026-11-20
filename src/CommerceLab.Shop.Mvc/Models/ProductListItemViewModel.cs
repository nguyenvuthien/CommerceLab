namespace CommerceLab.Shop.Mvc.Models;

public class ProductListItemViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public string PriceText { get; set; } = string.Empty;
}