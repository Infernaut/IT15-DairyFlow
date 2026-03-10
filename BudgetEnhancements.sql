-- =====================================================
-- DairyFlow: Budget Enhancements Migration
-- Adds Category to Budget, and Category/Emergency
-- fields to Expense for monthly budget enforcement.
-- =====================================================

-- 1. Add Category column to Budget table
IF NOT EXISTS (
    SELECT * FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.Budget') AND name = 'Category'
)
BEGIN
    ALTER TABLE [dbo].[Budget]
    ADD [Category] NVARCHAR(50) NOT NULL DEFAULT 'Other';

    PRINT 'Added Category column to Budget table.';
END
ELSE
    PRINT 'Budget.Category column already exists.';
GO

-- 2. Add unique index on Budget (CompanyID, Period, Category)
--    Ensures only one budget limit per company per month per category
IF NOT EXISTS (
    SELECT * FROM sys.indexes
    WHERE object_id = OBJECT_ID('dbo.Budget') AND name = 'IX_Budget_Company_Period_Category'
)
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX [IX_Budget_Company_Period_Category]
    ON [dbo].[Budget]([CompanyID], [Period], [Category]);

    PRINT 'Created unique index IX_Budget_Company_Period_Category.';
END
ELSE
    PRINT 'Index IX_Budget_Company_Period_Category already exists.';
GO

-- 3. Add Category column to Expense table
IF NOT EXISTS (
    SELECT * FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.Expense') AND name = 'Category'
)
BEGIN
    ALTER TABLE [dbo].[Expense]
    ADD [Category] NVARCHAR(50) NULL DEFAULT 'Other';

    PRINT 'Added Category column to Expense table.';
END
ELSE
    PRINT 'Expense.Category column already exists.';
GO

-- 4. Add IsEmergency column to Expense table
IF NOT EXISTS (
    SELECT * FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.Expense') AND name = 'IsEmergency'
)
BEGIN
    ALTER TABLE [dbo].[Expense]
    ADD [IsEmergency] BIT NOT NULL DEFAULT 0;

    PRINT 'Added IsEmergency column to Expense table.';
END
ELSE
    PRINT 'Expense.IsEmergency column already exists.';
GO

-- 5. Add EmergencyReason column to Expense table
IF NOT EXISTS (
    SELECT * FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.Expense') AND name = 'EmergencyReason'
)
BEGIN
    ALTER TABLE [dbo].[Expense]
    ADD [EmergencyReason] NVARCHAR(500) NULL;

    PRINT 'Added EmergencyReason column to Expense table.';
END
ELSE
    PRINT 'Expense.EmergencyReason column already exists.';
GO

-- 6. Add index on Expense (CompanyID, Category) for budget lookups
IF NOT EXISTS (
    SELECT * FROM sys.indexes
    WHERE object_id = OBJECT_ID('dbo.Expense') AND name = 'IX_Expense_Company_Category'
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Expense_Company_Category]
    ON [dbo].[Expense]([CompanyID], [Category]);

    PRINT 'Created index IX_Expense_Company_Category.';
END
ELSE
    PRINT 'Index IX_Expense_Company_Category already exists.';
GO

PRINT 'Budget enhancements migration complete.';
GO
