# EF Core migrations
Môi trường tạo project không có .NET SDK để sinh migration binary/code tự động. Ứng dụng dùng `Database.EnsureCreatedAsync()` để tạo database schema từ model khi chạy F5. Khi có .NET 8 SDK trên máy, có thể chuyển sang migration chuẩn:

```powershell
Add-Migration InitialCreate
Update-Database
```

Sau đó có thể bỏ `EnsureCreatedAsync()` trong Program.cs và thay bằng `Database.MigrateAsync()`.
