using CommerceLab.Api.Models;

namespace CommerceLab.Api.Services;

public class ProductService
{
    public ProductDto[] GetProducts()
    {
        return new[]
        {
            new ProductDto
            {
                Id = 1,
                Name = "Chuột không dây",
                Price = 500000m
            },
            new ProductDto
            {
                Id = 2,
                Name = "Bàn phím",
                Price = 650000m
            },
            new ProductDto
            {
                Id = 3,
                Name = "Tai nghe",
                Price = 150000m
            },
            new ProductDto
            {
                Id = 4,
                Name = "Tai nghe có dây",
                Price = 250000m
            },
            new ProductDto
            {
                Id = 5,
                Name = "Màn hình",
                Price = 350000m
            }
        };
    }

    public ProductDto? GetById(int id)
    {
        return GetProducts()
            .FirstOrDefault(product => product.Id == id);
    }

    public ProductPageDto GetPage(int page, int pageSize, string sort)
    {
        var source = GetProducts();

        var ordered = source.OrderBy(product => product.Id);

        if (sort == "price_asc")
        {
            ordered = source
                .OrderBy(product => product.Price)
                .ThenBy(product => product.Id);
        }
        else if (sort == "price_desc")
        {
            ordered = source
                .OrderByDescending(product => product.Price)
                .ThenBy(product => product.Id);
        }

        var products = ordered.ToArray();

        long offset = ((long)page - 1) * pageSize;

        var items = offset >= products.Length
            ? Array.Empty<ProductDto>()
            : products
                .Skip((int)offset)
                .Take(pageSize)
                .ToArray();

        return new ProductPageDto
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = products.Length
        };
    }
}