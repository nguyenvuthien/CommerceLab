# Nhật ký tuần 1 — MVC gọi API và xử lý lỗi

Ngày cập nhật: 20/11/2026.

Nhật ký được tổng hợp từ mã nguồn hiện tại, kết quả người học đã xác nhận và các phép kiểm tra được ghi rõ nguồn bên dưới. Ngày 20/11/2026, trợ giảng gọi HTTP trực tiếp để kiểm tra luồng đã phục hồi sau thí nghiệm contract; chưa chạy toàn bộ smoke script. Các mục chưa có kết quả xác nhận được ghi riêng.

## 1. Mục tiêu và yêu cầu

Mục tiêu đang thực hiện: khách hàng xem được tên và giá sản phẩm trên trang MVC; khi API không hoạt động, trang thông báo rõ việc tạm thời không tải được dữ liệu.

Các nhu cầu đã nêu cho CommerceLab:

- Khách hàng xem giá để quyết định mua.
- Khách hàng tìm kiếm sản phẩm.
- Khách hàng đặt hàng.
- Nhân viên xem đơn hàng khách đã đặt.
- Nhân viên thống kê mặt hàng đã bán.

Tuần này mới thực hiện luồng xem sản phẩm. Mức độ sử dụng được giả định là cơ bản. Tìm kiếm, đặt hàng, báo cáo và đăng nhập chưa được triển khai.

### Tiêu chí chất lượng và giả định tải đã học

| Tiêu chí chất lượng | Cách kiểm tra | Trạng thái |
|---|---|---|
| API ngừng thì MVC thông báo rõ và trả 503 | Tắt API, tải lại MVC | Người học đã báo hoàn thành |
| API hoạt động lại thì trang phục hồi mà không khởi động lại MVC | Bật API, tải lại MVC | Người học đã báo hoàn thành |
| Đặt thời gian chờ lời gọi API là 5 giây | Cho API cố ý phản hồi chậm rồi quan sát | Có cấu hình, chưa thử riêng nhánh timeout |

Giả định phục vụ bài học: 100 người dùng hoạt động/ngày, mỗi người mở trang sản phẩm 10 lần/ngày. Mỗi lần MVC tạo trang gọi API danh sách một lần, chưa có cache hoặc retry. Đây là mô hình giả định, không phải số đo sử dụng thực tế.

- Lượt mở trang/ngày: `100 × 10 = 1.000`.
- RPS trung bình cho request trang sản phẩm: `1.000 / 86.400 ≈ 0,0116`.
- Giả định giờ cao điểm gấp 20 lần trung bình: `0,0116 × 20 ≈ 0,23 RPS`.
- Request HTML, request tài nguyên tĩnh và request MVC → API cần được đếm riêng. Các con số này chưa chứng minh khả năng chịu tải.

Người học đã trả lời đúng bài kiểm tra: nếu giả sử 1.000 người/ngày, mỗi người 10 lượt mở trang thì có 10.000 lượt/ngày và RPS trung bình khoảng 0,116. Người học cũng phân loại đúng yêu cầu API ngừng → MVC trả 503 là yêu cầu chất lượng.

## 2. Môi trường và cách chạy

- Thư mục dự án: `D:\Source\CommerceLab`.
- Solution: `CommerceLab.slnx`.
- Hai ứng dụng ASP.NET Core dùng C#, target framework `net10.0`.
- API chạy ở `http://localhost:5101`; MVC chạy ở `http://localhost:5102`.
- Sản phẩm được tạo trong bộ nhớ bởi `ProductService.GetProducts()`; hiện có 3 sản phẩm. Chưa có cơ sở dữ liệu.
- Đã có commit `20e4bfc` — `Refactor code structure for improved readability and maintainability`, được kiểm tra ngày 20/11/2026. Các cập nhật nhật ký sau commit này chưa nằm trong commit đó.

Mở hai terminal PowerShell, mỗi terminal chạy một ứng dụng.

Terminal API:

```powershell
Set-Location 'D:\Source\CommerceLab'
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet run --no-launch-profile --project src/CommerceLab.Api --urls http://localhost:5101
```

Terminal MVC:

```powershell
Set-Location 'D:\Source\CommerceLab'
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet run --no-launch-profile --project src/CommerceLab.Shop.Mvc --urls http://localhost:5102
```

Các địa chỉ dùng trong bài:

| Địa chỉ | Mục đích |
|---|---|
| `http://localhost:5101/api/products` | Danh sách sản phẩm dạng JSON |
| `http://localhost:5101/api/products/1` | Một sản phẩm có tồn tại |
| `http://localhost:5101/api/products/999` | Kiểm tra sản phẩm không tồn tại, trả 404 |
| `http://localhost:5101/openapi/v1.json` | Tài liệu OpenAPI, được bật trong Development |
| `http://localhost:5102/` | Trang MVC hiển thị sản phẩm |

## 3. Luồng xử lý trong code hiện tại

```text
Trình duyệt gửi GET / đến MVC :5102
  → HomeController.Index nhận request
  → IHttpClientFactory tạo HttpClient theo cấu hình CommerceLabApi
  → MVC gửi GET /api/products đến API :5101
  → ProductsController nhận ProductService qua DI
  → ProductService cung cấp sản phẩm
  → API trả JSON từ ProductDto[]
  → MVC đọc JSON, tạo List<ProductListItemViewModel>
  → Razor Views/Home/Index.cshtml tạo HTML
  → MVC trả HTML về trình duyệt
```

Đây là SSR: HTML chứa dữ liệu được tạo tại máy chủ MVC. Lời gọi MVC → API chạy trên máy chủ, nên không xuất hiện như một yêu cầu Fetch/XHR do trình duyệt gửi đến cổng 5101. Ngày 20/11/2026, người học xác nhận thấy tên sản phẩm trong View Source; phần quan sát Network chưa có xác nhận riêng.

MVC và API là hai process riêng trong bài học. Ranh giới HTTP giúp thực hành contract và lỗi mạng, đồng thời tạo thêm thời gian chờ và phụ thuộc vào ứng dụng API. Đây là lựa chọn của bài học; ứng dụng MVC khác có thể gọi tầng nghiệp vụ trong cùng process.

## 4. Kiến thức gắn với code đã viết

| Khái niệm | Ví dụ cụ thể trong CommerceLab |
|---|---|
| Routing | `[Route("api/products")]` và `[HttpGet("{id:int}")]` chọn endpoint theo URL và HTTP method. |
| DTO | `ProductDto` chứa `Id`, `Name`, `Price` để truyền qua API. `Price` dùng `decimal`. |
| Đăng ký DI | `builder.Services.AddScoped<ProductService>()` trong API đăng ký dịch vụ. |
| Constructor injection | `ProductsController(ProductService productService)` nhận dịch vụ từ DI. MVC nhận `IHttpClientFactory` và `ILogger<HomeController>` theo cách tương tự. |
| Named HttpClient | `AddHttpClient("CommerceLabApi", ...)` cấu hình địa chỉ gốc và timeout; `CreateClient("CommerceLabApi")` sử dụng cấu hình đó. |
| Async/await | `await client.GetStringAsync(...)` chờ kết quả HTTP mà không giữ một luồng xử lý đứng chờ thao tác mạng. |
| ViewModel | `ProductListItemViewModel` chứa `Name` và `PriceText` dành cho giao diện, ví dụ giá đã định dạng thành `500.000 đ`. |
| Razor View | `@model` khai báo kiểu dữ liệu; `Model` là dữ liệu được truyền qua `View(products)`; `@foreach` tạo các dòng HTML. |
| Logging | `_logger.LogWarning(exception, ...)` ghi chi tiết lỗi để chẩn đoán; trang người dùng hiển thị thông báo ngắn. |
| Cancellation | `HttpContext.RequestAborted` được truyền xuống lời gọi HTTP; bộ lọc `catch` không xử lý việc khách hủy yêu cầu như timeout. |

## 5. Các phép thử và kết quả đã báo cáo

Các kết quả dưới đây do người học xác nhận trong cuộc trao đổi, trừ những dòng ghi rõ trợ giảng kiểm tra trực tiếp. Chưa lưu ảnh chụp Network, bản log hoặc báo cáo kiểm thử tự động vào repository.

| Phép thử | Kết quả người học đã xác nhận |
|---|---|
| Gọi danh sách và chi tiết sản phẩm; gọi id không tồn tại | Các trường hợp hoạt động đúng sau khi thêm sản phẩm Tai nghe; trường hợp không tồn tại trả 404. |
| Cho MVC gọi API | MVC nhận được JSON sản phẩm. |
| Hiển thị qua Razor | Trang MVC hiển thị bảng tên và giá sản phẩm. |
| Sửa giá trong code API, lưu/chạy lại API rồi tải lại MVC | Giá trên MVC cập nhật theo API. Đây là thay đổi dữ liệu mẫu trong code, chưa phải chức năng cập nhật giá qua HTTP. |
| Tắt API, giữ MVC hoạt động | Người học báo hoàn thành bài kiểm tra trang thông báo và HTTP 503. |
| Bật lại API, tải lại trang MVC | Người học báo hoàn thành kiểm tra phục hồi, không cần khởi động lại MVC. |
| Xem mã nguồn HTML của trang MVC | Ngày 20/11/2026, người học xác nhận có tên sản phẩm trong View Source. |
| Dừng riêng MVC, giữ API chạy | Ngày 20/11/2026, người học xác nhận API vẫn trả JSON. |
| Đổi tên trường JSON từ `price` thành `unitPrice` | Người học gửi trang lỗi chung của MVC. Trợ giảng kiểm tra API vẫn trả 200 với `unitPrice`, trong khi MVC đọc `GetProperty("price")`; chưa thu được log exception gốc của request lỗi đó. |
| Khôi phục contract về `price` | Ngày 20/11/2026, trợ giảng gọi HTTP trực tiếp: API trả 200 với 3 sản phẩm có các trường `id`, `name`, `price`; MVC trả 200, HTML có bảng và tên Tai nghe. |

Hành vi xử lý lỗi trong code:

- Timeout phía MVC được đặt là 5 giây.
- `HttpRequestException` khi gọi API được ghi log và chuyển thành trang `CatalogUnavailable` với HTTP 503.
- `OperationCanceledException` khi request từ khách chưa bị hủy được xử lý thành trang 503.
- Trang thông báo có nút Thử lại để yêu cầu MVC tải sản phẩm lần nữa.
- Lỗi đọc JSON nằm ngoài hai khối `catch` trên; không được chuyển thành danh sách rỗng.

Chưa có xác nhận riêng cho thí nghiệm API cố ý phản hồi chậm quá 5 giây hoặc khách hủy request. Tắt API kiểm tra lỗi kết nối, không đủ để kết luận nhánh timeout đã được thử.

## 6. Một lỗi thực tế và cách sửa

Lỗi đã gặp:

```text
Unable to resolve service for type
'CommerceLab.Api.Services.ProductService'
while attempting to activate
'CommerceLab.Api.Controllers.ProductsController'.
```

Nguyên nhân: Controller yêu cầu `ProductService` qua hàm tạo, nhưng DI chưa có đăng ký tương ứng. `using` chỉ giúp tham chiếu tên kiểu; không đăng ký dịch vụ.

Cách sửa: thêm đăng ký trước `builder.Build()` trong API, rồi chạy lại:

```csharp
builder.Services.AddScoped<ProductService>();
```

Điều rút ra: cần phân biệt việc đăng ký dịch vụ trong `Program.cs` và việc nhận dịch vụ qua constructor của Controller. Code có thể biên dịch nhưng vẫn lỗi lúc framework tạo Controller để xử lý request.

## 7. Các quyết định và giới hạn hiện tại

- Giữ cấu trúc đơn giản để học luồng request, DI và HTTP trước khi tách module theo lộ trình.
- MVC đang đọc JSON bằng `JsonDocument` và tên trường `name`, `price`. Đây là bước thực hành tạm; nếu contract đổi, phần đọc này có thể lỗi lúc chạy. Client sinh tự động chưa được tích hợp.
- MVC dùng ViewModel riêng cho giao diện. Không tham chiếu `ProductService` hoặc dữ liệu nội bộ của API.
- OpenAPI đã mô tả danh sách, chi tiết và 404; hai thao tác sản phẩm đã có tên `GetProducts` và `GetProductById`. Việc tùy chỉnh schema `decimal` chưa được áp dụng trong `Program.cs`.
- Route thực hành là `/api/products`, khác prefix `/api/v1/products` của starter trong khóa học. Chưa đổi route hoặc chạy smoke script của starter trên dự án này.
- Dữ liệu mẫu hiện có 3 sản phẩm; mốc bài mẫu trong workbook là 5 sản phẩm.
- Chưa có lưu trữ bền, tìm kiếm, đặt hàng, phân quyền, generated client hoặc benchmark. Giả định tải ban đầu đã được ghi ở mục 1; chưa ước lượng dung lượng lưu trữ hoặc kiểm chứng bằng phép đo tải.

## 8. Việc còn lại để chốt tuần 1

- [x] Xây API danh sách/chi tiết và xử lý sản phẩm không tồn tại.
- [x] Nối MVC với API, hiển thị bảng và quan sát thay đổi giá.
- [x] Người học báo hoàn thành kiểm tra API offline → MVC 503 → phục hồi.
- [x] Ghi nhật ký và một lỗi DI thực tế.
- [x] Người học xác nhận tên sản phẩm có trong View Source.
- [x] Người học xác nhận dừng MVC thì API vẫn trả JSON.
- [ ] Quan sát Network để xác nhận lời gọi MVC → API không xuất hiện như Fetch/XHR từ trình duyệt.
- [ ] Tự giải thích vì sao luồng MVC → API hiện tại không cần CORS.
- [ ] Hoàn thiện yêu cầu, phạm vi và giả định tải có ghi rõ cách tính.
- [ ] Vẽ Context Diagram và Container Diagram cho đúng các thành phần hiện có.
- [ ] Bổ sung dữ liệu mẫu lên 5 sản phẩm theo workbook.
- [x] Đã thử thay đổi contract, quan sát trang lỗi MVC và kiểm tra trực tiếp luồng phục hồi sau khi trả về contract cũ.
- [ ] Bổ sung README hướng dẫn chạy.
- [x] Đã có commit lưu mã nguồn thực hành (`20e4bfc`).

Các nội dung tuần 2 như pagination, validation và request log sẽ thực hiện sau khi chốt phần lõi tuần 1. Bài sinh client từ OpenAPI là phần tùy chọn của tuần 2, chưa coi là điều kiện hoàn thành tuần 1.
