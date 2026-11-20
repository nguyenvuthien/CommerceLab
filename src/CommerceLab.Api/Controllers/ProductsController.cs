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

    [HttpGet("{id:int}", Name = "GetProductById")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<ProductDto> GetById(int id)
    {
        if (id <= 0)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "ID sản phẩm không hợp lệ",
                Detail = "ID sản phẩm phải lớn hơn 0."
            });
        }

        var product = _productService.GetById(id);

        if (product == null)
        {
            return NotFound();
        }

        return Ok(product);
    }

    [HttpGet(Name = "GetProducts")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<ProductPageDto> Get(
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 10,
    [FromQuery] string sort = "id")
    {
        if (page < 1 || pageSize < 1 || pageSize > 100)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Tham số phân trang không hợp lệ",
                Detail = "page phải từ 1 trở lên; pageSize phải từ 1 đến 100."
            });
        }

        if (sort != "id"
            && sort != "price_asc"
            && sort != "price_desc")
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Cách sắp xếp không hợp lệ",
                Detail = "sort phải là id, price_asc hoặc price_desc."
            });
        }

        var result = _productService.GetPage(page, pageSize, sort);

        return Ok(result);
    }
}