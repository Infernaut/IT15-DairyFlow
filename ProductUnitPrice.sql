-- =====================================================
-- DairyFlow: Product Column Additions
-- Adds ShelfLifeDays and UnitPrice columns to Product.
-- =====================================================

-- 1. Add ShelfLifeDays column to Product table
IF NOT EXISTS (
    SELECT * FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.Product') AND name = 'ShelfLifeDays'
)
BEGIN
    ALTER TABLE [dbo].[Product]
    ADD [ShelfLifeDays] INT NULL;

    PRINT 'Added ShelfLifeDays column to Product table.';
END
ELSE
    PRINT 'Product.ShelfLifeDays column already exists.';
GO

-- 2. Add UnitPrice column to Product table
IF NOT EXISTS (
    SELECT * FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.Product') AND name = 'UnitPrice'
)
BEGIN
    ALTER TABLE [dbo].[Product]
    ADD [UnitPrice] DECIMAL(18, 2) NULL;

    PRINT 'Added UnitPrice column to Product table.';
END
ELSE
    PRINT 'Product.UnitPrice column already exists.';
GO

PRINT 'Product column additions complete.';
GO
