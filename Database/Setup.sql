IF DB_ID(N'LUMIPHUXDb') IS NULL
BEGIN
    CREATE DATABASE LUMIPHUXDb;
END
GO

USE LUMIPHUXDb;
GO

PRINT 'LUMIPHUXDb is ready. Run the application once to let EF Core EnsureCreatedAsync create the full Identity/application schema and seed demo data.';
GO
