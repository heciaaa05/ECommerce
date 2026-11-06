# ECommerce – Website bán Gaming Gear
Backend thương mại điện tử cho cửa hàng **gaming gear** (chuột, bàn phím, tai nghe...), xây dựng bằng **ASP.NET Core theo Clean Architecture**. Đây là dự án portfolio, phát triển theo lộ trình 8 tuần; phần AI (gợi ý sản phẩm, chatbot RAG) sẽ là một service Python riêng ở các tuần sau.

## Trạng thái dự án

| Tuần | Nội dung | Trạng thái |
|---|---|---|
| 1–2 | Auth/JWT, Sản phẩm, Danh mục, Giỏ hàng (Redis), Đơn hàng | Hoàn thành, đã test qua Swagger |
| 3 | Thanh toán VNPay sandbox (IPN, kiểm tra chữ ký, idempotency) | Logic đã test bằng IPN giả; chờ VNPay duyệt website để thanh toán thật |
| 4 | Redis caching + Docker hoá + health check | Hoàn thành, Docker Compose chạy ổn |
| 5 | Gợi ý sản phẩm content-based (Python FastAPI + TF-IDF) | Code xong, chờ test trên Docker |
| 6 | Gợi ý hybrid (kết hợp hành vi người dùng) | Chưa làm |
| 7 | Chatbot RAG | Chưa làm |
| 8 | CI/CD + tài liệu kiến trúc | Chưa làm |

## Công nghệ

- **.NET 10**, ASP.NET Core Web API, **MediatR** (CQRS)
- **EF Core 10** + **SQL Server 2022** (Docker)
- **Redis**: cache sản phẩm/danh mục và lưu giỏ hàng
- **JWT** (access token 15 phút, refresh token 7 ngày) + **BCrypt** băm mật khẩu
- **VNPay sandbox** (HMAC-SHA512) + **ngrok** để nhận IPN trên máy local
- **Swagger** (Swashbuckle) để test API
- **Docker / Docker Compose**

## Kiến trúc

```
ECommerce.API             Controllers, Program.cs, job nền huỷ đơn quá hạn, health check
ECommerce.Application     CQRS (Commands/Queries), DTO, interface
ECommerce.Infrastructure  EF Core, Redis, JWT, VNPay
ECommerce.Domain          Entity, Enum (không phụ thuộc project nào)
```

Chiều phụ thuộc: `API → Infrastructure → Application → Domain`.

## Tính năng

- **Xác thực**: đăng ký, đăng nhập, phân quyền `Customer` / `Seller` / `Admin`
- **Sản phẩm**: CRUD, biến thể (SKU, giá, tồn kho, thuộc tính), ảnh, lọc theo danh mục/thương hiệu/từ khoá, phân trang, xoá mềm
- **Giỏ hàng**: lưu trong Redis (hết hạn sau 7 ngày)
- **Đơn hàng**: tạo từ giỏ hàng, **trừ tồn kho ngay khi đặt**, hoàn lại khi huỷ hoặc quá hạn
- **Thanh toán VNPay**: tạo link thanh toán, nhận IPN, chỉ **IPN** mới được cập nhật trạng thái đơn
- **Huỷ đơn tự động**: đơn `Pending` quá **15 phút** chưa thanh toán sẽ bị huỷ và hoàn tồn kho (job nền, chạy mỗi phút)
- **Cache Redis**: chi tiết sản phẩm 30 phút, danh sách sản phẩm 5 phút, danh mục 1 giờ; tự xoá khi dữ liệu đổi; Redis lỗi thì app vẫn chạy bằng DB
- **Gợi ý sản phẩm tương tự**: service Python (FastAPI + scikit-learn) tính độ giống bằng TF-IDF trên tên, thương hiệu, danh mục, mô tả, thuộc tính, kết hợp độ gần về giá; service lỗi thì API trả sản phẩm cùng danh mục
- **Dữ liệu mẫu**: 32 sản phẩm gaming gear tự nạp khi DB còn trống (`Database__SeedDemoData=true`)
- **Health check**: `/health` (còn sống), `/health/ready` (SQL Server + Redis)

## Các API chính

| Nhóm | Endpoint | Quyền |
|---|---|---|
| Auth | `POST /api/Auth/register`, `POST /api/Auth/login` | Công khai |
| Danh mục | `GET /api/Categories` | Công khai |
| | `POST /api/Categories` | Admin |
| Sản phẩm | `GET /api/Products`, `GET /api/Products/{id}` | Công khai |
| | `GET /api/Products/{id}/recommendations?top=5` | Công khai |
| | `POST`, `PUT /{id}`, `DELETE /{id}` | Admin, Seller |
| Giỏ hàng | `GET /api/Cart`, `POST/PUT /api/Cart/items`, `DELETE /api/Cart/items/{variantId}` | Đăng nhập |
| Đơn hàng | `GET /api/Orders`, `GET /api/Orders/{id}`, `POST /api/Orders`, `POST /api/Orders/{id}/cancel` | Đăng nhập |
| Thanh toán | `POST /api/payments/vnpay/create/{orderId}` | Đăng nhập |
| | `GET /api/payments/vnpay/ipn`, `GET /api/payments/vnpay/return` | Công khai (VNPay gọi) |

Xem đầy đủ và thử trực tiếp tại Swagger: `http://localhost:5203/swagger`.

## Chạy trên máy local

### 1. Cài công cụ

- [.NET SDK 10](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (Windows cần chạy `wsl --install` trước, rồi khởi động lại)
- Công cụ EF: `dotnet tool install --global dotnet-ef`

### 2. Cấu hình khoá bí mật (User Secrets)

Không ghi khoá thật vào `appsettings.json`. Dùng User Secrets (ID cố định `ecommerce-api-secrets` đã khai báo trong `ECommerce.API.csproj`):

```powershell
cd src\ECommerce.API
dotnet user-secrets set "VnPay:TmnCode" "<TmnCode trong email VNPay>"
dotnet user-secrets set "VnPay:HashSecret" "<HashSecret trong email VNPay>"
dotnet user-secrets set "JwtSettings:Secret" "<chuoi-bi-mat-dai-it-nhat-32-ky-tu>"
cd ..\..
```

Nếu báo thiếu `UserSecretsId`, chạy thêm `dotnet user-secrets init --id ecommerce-api-secrets`.

### 3. Dựng database, Redis và build

```powershell
powershell -ExecutionPolicy Bypass -File .\setup.ps1
```

Script sẽ tạo container SQL Server (`sql-ecommerce`) và Redis (`redis-ecommerce`), build, tạo migration `InitialCreate` (nếu chưa có) và cập nhật database.

### 4. Chạy API

```powershell
dotnet run --project src\ECommerce.API
```

Swagger: `http://localhost:5203/swagger`

## Chạy toàn bộ bằng Docker Compose

```powershell
docker stop sql-ecommerce redis-ecommerce
Copy-Item .env.example .env
notepad .env
docker compose up -d --build
docker compose ps
```

- `.env` cần điền `VNPAY_TMNCODE`, `VNPAY_HASHSECRET`, `VNPAY_RETURN_URL` và nên đổi `JWT_SECRET`. File này đã nằm trong `.gitignore`.
- API trong container tự áp migration khi khởi động (cần có thư mục `Migrations`, được tạo sau khi chạy `setup.ps1`).
- Dữ liệu SQL Server và Redis nằm trong volume `sql-data`, `redis-data` nên xoá container không mất dữ liệu.
- Xem log: `docker compose logs -f api`. Dừng: `docker compose down` (thêm `-v` nếu muốn xoá cả dữ liệu).
- Kiểm tra: `http://localhost:5203/health` và `http://localhost:5203/health/ready`.

## Gợi ý sản phẩm (service Python)

```
Trình duyệt → ASP.NET API → GET /recommend/{id} → Recommender (FastAPI)
                  ↑                                      │
                  └──── GET /api/Products (nạp catalog, làm mới mỗi 5 phút) ◄─┘
```

- Điểm gợi ý = 0.70 × cosine(TF-IDF) + 0.15 × độ gần về giá + 0.15 × cùng danh mục; bỏ qua sản phẩm hết hàng
- Mỗi gợi ý có `reasons` (`same_category`, `same_brand`, `similar_price`) để giải thích
- API cache kết quả 10 phút; service Python lỗi thì trả sản phẩm mới nhất cùng danh mục (`fallback_same_category`)
- Chạy cùng Docker Compose (service `recommender`, cổng 8000): `http://localhost:8000/health`
- Chạy test: `cd recommender; pip install -r requirements.txt; python -m pytest -q`
- Thử: `GET http://localhost:5203/api/Products/{id}/recommendations?top=5`
## Test nhanh qua Swagger

1. `POST /api/Auth/register`, rồi nâng quyền Admin (xem bên dưới), rồi `POST /api/Auth/login` lại và bấm **Authorize** dán `accessToken`.
2. `POST /api/Categories` → lấy `categoryId` thật từ response.
3. `POST /api/Products` (kèm `variants`) → `GET /api/Products/{id}` để lấy `variants[].id`.
4. `POST /api/Cart/items` → `POST /api/Orders` → `POST /api/payments/vnpay/create/{orderId}`.
5. Mở `paymentUrl` trên trình duyệt và thanh toán bằng thẻ test; IPN về ngrok thì đơn chuyển sang `Paid`.

Nâng quyền Admin (token cũ vẫn mang role cũ nên **phải đăng nhập lại**). Thay email cho đúng:

```powershell
docker exec -it sql-ecommerce /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "YourStrong@Passw0rd" -C -Q "UPDATE ECommerceDb.dbo.Users SET Role = 2 WHERE Email = 'email-cua-ban@example.com'"
```

Quyền: `0` = Customer, `1` = Seller, `2` = Admin. Lưu ý `-P` là mật khẩu của tài khoản `sa` trong SQL Server, không phải mật khẩu đăng nhập web.

### Test IPN không cần cổng VNPay

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\fake-ipn.ps1 -OrderId <orderId-that> -Amount <tong-tien-VND>
```

Script giả lập VNPay gọi IPN với chữ ký hợp lệ (đọc khoá từ User Secrets). Thêm `-Fail` để giả lập thanh toán thất bại. Điều kiện: đơn còn `Pending` và đã gọi `POST /api/payments/vnpay/create/{orderId}`.

Kết quả: `00` đã cập nhật đơn, `02` đơn đã xác nhận trước đó, `97` sai chữ ký, `04` sai số tiền, `01` không thấy đơn hoặc chưa tạo payment, `99` đơn không còn ở trạng thái chờ.

## VNPay và ngrok

- Cần một domain công khai để VNPay gọi IPN về máy local. Chạy `ngrok http 5203` (ngrok cần phiên bản 3.28 trở lên cho tài khoản Free; kiểm tra bằng `ngrok version`).
- Cấu hình IPN URL tại trang `https://sandbox.vnpayment.vn/merchantv2/Account/TerminalEdit.htm`: `https://<domain-ngrok>/api/payments/vnpay/ipn`.
- `VnPay:ReturnUrl` trong `appsettings.json` (hoặc `VNPAY_RETURN_URL` trong `.env`) là `https://<domain-ngrok>/api/payments/vnpay/return`.
- Domain trong `ReturnUrl` phải khớp với website đã đăng ký và được VNPay duyệt cho TmnCode của bạn, nếu không VNPay báo "Website này chưa được phê duyệt".
- Nút "Test call IPN" của VNPay trả `RspCode 97` là **bình thường**, vì dữ liệu mẫu của họ không có chữ ký hợp lệ.
- Chỉ **IPN** mới được cập nhật trạng thái đơn. **Return URL** chỉ để hiển thị kết quả cho người dùng.

## Lưu ý kỹ thuật (đừng sửa lại)

- `Microsoft.OpenApi` phải giữ phiên bản **2.7.5**; bản 3.x gây lỗi CS0200 với .NET 10.
- Không dùng tuple để lưu giỏ hàng vào Redis (System.Text.Json bỏ qua field) → dùng record `CartItemRecord`.
- Namespace cổng thanh toán là `PaymentGateways` để không trùng với entity `Payment`.
- Access token sống 15 phút: Swagger báo `401 invalid_token` thì đăng nhập lại và Authorize lại.
- Id mẫu trong ô "Example Value" của Swagger không phải dữ liệu thật; luôn dùng id lấy từ response.
- Build báo `file is locked by ECommerce.API` nghĩa là API đang chạy; dừng bằng `Ctrl+C` rồi build lại.

## Bảo mật

- Không commit `HashSecret`, JWT `Secret`, ngrok authtoken hay file `.env`.
- Chuỗi kết nối và mật khẩu SQL trong `appsettings.json` chỉ dùng cho môi trường local. Production phải dùng biến môi trường hoặc secret manager.
- Nếu lỡ lộ khoá hoặc token ngrok, hãy thu hồi và tạo mới ngay.

## Giấy phép

Dự án cá nhân phục vụ học tập và portfolio.
