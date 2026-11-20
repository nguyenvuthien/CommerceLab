namespace CommerceLab.Api.Models;

public class ProductPageDto
{
    public ProductDto[] Items { get; set; } = Array.Empty<ProductDto>();

    public int Page { get; set; }

    public int PageSize { get; set; }

    public int TotalCount { get; set; }
}