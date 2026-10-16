USE BeautyCareShopDb;
GO

IF OBJECT_ID(N'dbo.RolePermissions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RolePermissions (
        RoleId NVARCHAR(450) NOT NULL,
        PermissionId NVARCHAR(450) NOT NULL,
        IsGranted BIT NOT NULL DEFAULT 0,
        CONSTRAINT PK_RolePermissions PRIMARY KEY (RoleId, PermissionId),
        CONSTRAINT FK_RolePermissions_SystemPermissions FOREIGN KEY (PermissionId) 
            REFERENCES dbo.SystemPermissions (PermissionId) ON DELETE CASCADE,
        CONSTRAINT FK_RolePermissions_AspNetRoles FOREIGN KEY (RoleId) 
            REFERENCES dbo.AspNetRoles (Id) ON DELETE CASCADE
    );
END
GO