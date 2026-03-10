-- Add ShelfLifeDays column to Product table
-- Used to auto-calculate product expiry date on QM release for food safety compliance (FDA/BFAD)
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Product' AND COLUMN_NAME = 'ShelfLifeDays')
BEGIN
    ALTER TABLE [Product] ADD [ShelfLifeDays] INT NULL;
END
GO
