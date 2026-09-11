# LUMIPHUX (LPX) — ASP.NET Core MVC .NET 8

Website thương mại điện tử Health & Beauty Care xây dựng bằng ASP.NET Core MVC, C#, SQL Server, Entity Framework Core, ASP.NET Identity, Bootstrap 5, AOS, JavaScript/AJAX và SignalR.

## Môi trường
- Visual Studio 2022
- Workload: ASP.NET and web development
- .NET 8 SDK
- SQL Server hoặc LocalDB

## Chạy
1. Mở `BeautyCareShop.sln`.
2. Restore NuGet Packages.
3. Kiểm tra `appsettings.json`. Mặc định: `(localdb)\\MSSQLLocalDB`, database `LUMIPHUXDb`.
4. F5. Startup dùng `EnsureCreatedAsync()` và tự seed dữ liệu.
5. Nếu bạn đã chạy phiên bản ZIP cũ trước đây, xóa database `LUMIPHUXDb` rồi chạy lại để schema được tạo mới; chương trình cũng có patch tự động thêm `Orders.InventoryReleasedAt` nếu cột còn thiếu.

## Tài khoản demo
- Owner: `admin@lumiphux.com` / `Admin@123456`
- ProductManager: `productmanager@lumiphux.com` / `Product@123456`
- InventoryManager: `warehouse@lumiphux.com` / `Warehouse@123456`
- CustomerService: `cskh@lumiphux.com` / `Cskh@123456`
- Customer: `customer@lumiphux.com` / `Customer@123456`

## Chức năng
- Luxury responsive UI, Hero carousel, AOS, categories, best sellers, flash sale, review, journal.
- Live Search AJAX + filter theo danh mục/giá.
- Product detail gallery, rating/review.
- Cart AJAX add + mini cart count + update/remove.
- Checkout COD / QR / Card demo.
- QR chứa mã đơn + số tiền và deep-link demo; đặt hàng online có 10 phút giữ chỗ.
- Payment Success page, popup, demo email logging.
- Hủy đơn và chọn lý do; hoàn lại tồn kho khi hủy/hết hạn thanh toán.
- ASP.NET Identity + Role-based access.
- Owner Dashboard.
- Product Manager CRUD cơ bản.
- Inventory Manager nhập/xuất/điều chỉnh tồn.
- CustomerService + SignalR live chat.
- AI Assistant rule-based có thể thực hiện các tác vụ trong website: tìm sản phẩm, kiểm tra tồn, thêm/xóa giỏ, mở checkout, tra cứu đơn, hỗ trợ hủy đơn.

## AI thật
Provider mặc định là `LocalRuleBased`. Có thể thay `SiteAssistantService` bằng OpenAI/Azure OpenAI hoặc provider khác. Không hard-code API key.

## Thanh toán production
Card/QR hiện là demo. Để production, tích hợp gateway chính thức (VNPay/MoMo/Stripe/VietQR hoặc đối tác của bạn), webhook/API callback, chữ ký/kiểm tra trạng thái giao dịch và không lưu số thẻ/CVV.

## EF Core Migration
Hiện project dùng `EnsureCreatedAsync()` để F5 nhanh. Khi muốn chuyển hẳn sang migration chuẩn:

```powershell
Add-Migration InitialCreate
Update-Database
```

## Build note
Môi trường tạo ZIP này không có Visual Studio/.NET SDK nên không thể tuyên bố đã build trên VS 2022. Hãy `Restore` và `Rebuild Solution` trên Windows/Visual Studio 2022 trước khi triển khai.
