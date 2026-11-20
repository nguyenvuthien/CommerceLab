using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using CommerceLab.Shop.Mvc.Models;
using System.Globalization;
using System.Text.Json;
using System.Net;

namespace CommerceLab.Shop.Mvc.Controllers;

public class HomeController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<HomeController> _logger;

    public HomeController(IHttpClientFactory httpClientFactory, ILogger<HomeController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }
    public async Task<IActionResult> Index(int page = 1, string sort = "id")
    {
        if (!ModelState.IsValid || page < 1)
        {
            return BadRequest("Số trang phải là số nguyên lớn hơn hoặc bằng 1.");
        }

        if (sort != "id"
            && sort != "price_asc"
            && sort != "price_desc")
        {
            return BadRequest("Cách sắp xếp không hợp lệ.");
        }

        const int pageSize = 2;

        using var client =
            _httpClientFactory.CreateClient("CommerceLabApi");

        string json;

        try
        {
            json = await client.GetStringAsync(
                $"api/products?page={page}&pageSize={pageSize}&sort={sort}",
                HttpContext.RequestAborted);
        }
        catch (HttpRequestException exception)
        {
            _logger.LogWarning(
                exception,
                "Không thể tải sản phẩm từ API.");

            Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            return View("CatalogUnavailable");
        }
        catch (OperationCanceledException exception)
            when (!HttpContext.RequestAborted.IsCancellationRequested)
        {
            _logger.LogWarning(
                exception,
                "Đã hết thời gian chờ API sản phẩm.");

            Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            return View("CatalogUnavailable");
        }

        using var document = JsonDocument.Parse(json);

        var products = new List<ProductListItemViewModel>();
        var culture = CultureInfo.GetCultureInfo("vi-VN");

        foreach (var item in document.RootElement.GetProperty("items").EnumerateArray())
        {
            var name = item.GetProperty("name").GetString();
            var price = item.GetProperty("price").GetDecimal();

            products.Add(new ProductListItemViewModel
            {
                Id = item.GetProperty("id").GetInt32(),
                Name = name ?? string.Empty,
                PriceText = price.ToString("N0", culture) + " đ"
            });
        }

        var model = new ProductListViewModel
        {
            Items = products,
            Page = document.RootElement.GetProperty("page").GetInt32(),
            PageSize = document.RootElement.GetProperty("pageSize").GetInt32(),
            TotalCount = document.RootElement.GetProperty("totalCount").GetInt32(),
            Sort = sort
        };

        if (model.Page > 1 && model.Items.Count == 0)
        {
            return RedirectToAction(nameof(Index), new { page = 1, sort });
        }

        return View(model);
    }

    [HttpGet]
public async Task<IActionResult> Details(int id)
{
    if (!ModelState.IsValid || id <= 0)
    {
        return BadRequest("ID sản phẩm phải là số nguyên lớn hơn 0.");
    }

    using var client =
        _httpClientFactory.CreateClient("CommerceLabApi");

    string json;

    try
    {
        json = await client.GetStringAsync(
            $"api/products/{id}",
            HttpContext.RequestAborted);
    }
    catch (HttpRequestException exception)
        when (exception.StatusCode == HttpStatusCode.NotFound)
    {
        Response.StatusCode = StatusCodes.Status404NotFound;
        return View("ProductNotFound");
    }
    catch (HttpRequestException exception)
    {
        _logger.LogWarning(
            exception,
            "Không thể tải sản phẩm {ProductId} từ API.",
            id);

        Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        return View("CatalogUnavailable");
    }
    catch (OperationCanceledException exception)
        when (!HttpContext.RequestAborted.IsCancellationRequested)
    {
        _logger.LogWarning(
            exception,
            "Hết thời gian chờ API khi tải sản phẩm {ProductId}.",
            id);

        Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        return View("CatalogUnavailable");
    }

    using var document = JsonDocument.Parse(json);
    var product = document.RootElement;

    var model = new ProductDetailsViewModel
    {
        Id = product.GetProperty("id").GetInt32(),
        Name = product.GetProperty("name").GetString() ?? string.Empty,
        PriceText = product.GetProperty("price")
            .GetDecimal()
            .ToString("N0", CultureInfo.GetCultureInfo("vi-VN")) + " đ"
    };

    return View(model);
}

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
