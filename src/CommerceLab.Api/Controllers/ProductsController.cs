using CommerceLab.Api.Services;
using CommerceLab.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace CommerceLab.Api.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly ProductService _productService;

    public ProductsController(ProductService productService)
    {
        _productService = productService;
    }

    [HttpGet(Name = "GetProducts")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<ProductDto[]> Get()
    {
        var products = _productService.GetProducts();

        return Ok(products);
    }

    [HttpGet("{id:int}", Name = "GetProductById")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<ProductDto> GetById(int id)
    {
        var product = _productService.GetById(id);

        if (product == null)
        {
            return NotFound();
        }

        return Ok(product);
    }
}