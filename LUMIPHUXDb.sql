USE LUMIPHUXDb;
GO

-- 1. Tạo bảng SystemPermissions nếu chưa tồn tại
IF OBJECT_ID(N'dbo.SystemPermissions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SystemPermissions (
        PermissionId NVARCHAR(450) NOT NULL PRIMARY KEY,
        PermissionName NVARCHAR(MAX) NOT NULL,
        ModuleGroup NVARCHAR(MAX) NOT NULL
    );
END
GO

-- 2. Tạo bảng UserPermissions nếu chưa tồn tại
IF OBJECT_ID(N'dbo.UserPermissions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserPermissions (
        UserId NVARCHAR(450) NOT NULL,
        PermissionId NVARCHAR(450) NOT NULL,
        IsGranted BIT NOT NULL DEFAULT 0,
        CONSTRAINT PK_UserPermissions PRIMARY KEY (UserId, PermissionId),
        CONSTRAINT FK_UserPermissions_SystemPermissions FOREIGN KEY (PermissionId) 
            REFERENCES dbo.SystemPermissions (PermissionId) ON DELETE CASCADE,
        CONSTRAINT FK_UserPermissions_AspNetUsers FOREIGN KEY (UserId) 
            REFERENCES dbo.AspNetUsers (Id) ON DELETE CASCADE
    );
END
GO

-- 3. Nạp danh sách quyền mẫu ban đầu
IF NOT EXISTS (SELECT 1 FROM dbo.SystemPermissions)
BEGIN
    INSERT INTO dbo.SystemPermissions (PermissionId, PermissionName, ModuleGroup)
    VALUES 
        ('CATALOG_MANAGE', N'Quản lý sản phẩm & Danh mục', N'Sản phẩm'),
        ('INVENTORY_MANAGE', N'Quản lý nhập/xuất kho', N'Kho hàng'),
        ('ORDER_PROCESS', N'Xử lý & Cập nhật đơn hàng', N'Đơn hàng'),
        ('SUPPORT_CHAT', N'CSKH & Trực LiveChat', N'Hỗ trợ'),
        ('VOUCHER_MANAGE', N'Quản lý khuyến mãi/Voucher', N'Marketing');
END
GO