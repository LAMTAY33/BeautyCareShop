# Database

Ứng dụng mặc định dùng LocalDB và `EnsureCreatedAsync()` trong `Program.cs` để có thể F5 nhanh mà không cần chạy Migration trước.

Connection string:

`Server=(localdb)\\MSSQLLocalDB;Database=LUMIPHUXDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true`

Khi chuyển sang migration chuẩn:

```powershell
Add-Migration InitialCreate
Update-Database
```

Sau đó thay `EnsureCreatedAsync()` bằng `MigrateAsync()`.
