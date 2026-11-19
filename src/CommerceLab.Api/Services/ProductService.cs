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
                Price = 450000m
            }
        };
    }

    public ProductDto? GetById(int id)
    {
        return GetProducts()
            .FirstOrDefault(product => product.Id == id);
    }
}