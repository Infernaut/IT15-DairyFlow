-- =============================================
-- DairyFlow Finance, Inventory & Equipment SQL Script
-- Generated: March 1, 2026
-- Description: Schema changes for raw materials, 
-- inventory management, equipment, and finance updates
-- =============================================

-- =============================================
-- PART 1: RawMaterial Table - Add New Columns
-- =============================================

-- Add Unit column
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'RawMaterial') AND name = 'Unit')
BEGIN
    ALTER TABLE [RawMaterial] ADD [Unit] NVARCHAR(50) NULL DEFAULT 'kg';
END
GO

-- Add CurrentStock column
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'RawMaterial') AND name = 'CurrentStock')
BEGIN
    ALTER TABLE [RawMaterial] ADD [CurrentStock] INT NULL DEFAULT 0;
END
GO

-- Add MinimumStock column
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'RawMaterial') AND name = 'MinimumStock')
BEGIN
    ALTER TABLE [RawMaterial] ADD [MinimumStock] INT NULL DEFAULT 10;
END
GO

-- Add LastRestockDate column
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'RawMaterial') AND name = 'LastRestockDate')
BEGIN
    ALTER TABLE [RawMaterial] ADD [LastRestockDate] DATETIME2 NULL;
END
GO

-- Add CreatedAt column
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'RawMaterial') AND name = 'CreatedAt')
BEGIN
    ALTER TABLE [RawMaterial] ADD [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE();
END
GO

-- =============================================
-- PART 2: Equipment Table - Ensure Exists with All Columns
-- (May already exist from previous migration)
-- =============================================

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'Equipment') AND type = 'U')
BEGIN
    CREATE TABLE [Equipment] (
        [EquipmentID] INT IDENTITY(1,1) NOT NULL,
        [CompanyID] INT NOT NULL,
        [EquipmentName] NVARCHAR(200) NOT NULL,
        [EquipmentType] NVARCHAR(100) NULL,
        [Location] NVARCHAR(200) NULL,
        [Status] NVARCHAR(50) NULL DEFAULT 'Operational',
        [LastMaintenanceDate] DATETIME2 NULL,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        
        CONSTRAINT [PK_Equipment] PRIMARY KEY CLUSTERED ([EquipmentID] ASC),
        CONSTRAINT [FK_Equipment_Company_CompanyID] FOREIGN KEY ([CompanyID]) REFERENCES [Company]([CompanyID]) ON DELETE NO ACTION
    );
    
    CREATE NONCLUSTERED INDEX [IX_Equipment_CompanyID] ON [Equipment]([CompanyID]);
END
GO

-- =============================================
-- PART 3: Expense Table - Add Category and Description columns
-- =============================================

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'Expense') AND name = 'Category')
BEGIN
    ALTER TABLE [Expense] ADD [Category] NVARCHAR(100) NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'Expense') AND name = 'Description')
BEGIN
    ALTER TABLE [Expense] ADD [Description] NVARCHAR(500) NULL;
END
GO

-- =============================================
-- PART 4: Update existing RawMaterial records
-- =============================================

-- Set default values for existing records
UPDATE [RawMaterial]
SET [Unit] = 'kg'
WHERE [Unit] IS NULL;
GO

UPDATE [RawMaterial]  
SET [CurrentStock] = 0
WHERE [CurrentStock] IS NULL;
GO

UPDATE [RawMaterial]
SET [MinimumStock] = 10
WHERE [MinimumStock] IS NULL;
GO

-- =============================================
-- PART 5: Create Indexes for Performance
-- =============================================

-- Index on RawMaterial for stock queries
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_RawMaterial_CompanyID_CurrentStock' AND object_id = OBJECT_ID('RawMaterial'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_RawMaterial_CompanyID_CurrentStock] ON [RawMaterial]([CompanyID], [CurrentStock]);
END
GO

-- Index on Expense for date queries
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Expense_CompanyID_ExpenseDate' AND object_id = OBJECT_ID('Expense'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Expense_CompanyID_ExpenseDate] ON [Expense]([CompanyID], [ExpenseDate]);
END
GO

-- =============================================
-- NOTE: Budget table is being deprecated
-- No changes needed, but consider removing in future cleanup
-- =============================================

-- =============================================
-- Script Complete
-- =============================================
PRINT 'DairyFlow Finance, Inventory & Equipment enhancements applied successfully.';
GO
