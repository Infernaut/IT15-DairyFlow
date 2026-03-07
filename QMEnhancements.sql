-- =============================================
-- DairyFlow QM Enhancements SQL Script
-- Generated: February 28, 2026
-- Description: New tables and columns for Quality Management
-- =============================================

-- =============================================
-- PART 1: QualityInspection Table - Add New Columns
-- =============================================

-- Add Inspection Type (Incoming, InProcess, FinalProduct, Environmental)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'QualityInspection') AND name = 'InspectionType')
BEGIN
    ALTER TABLE [QualityInspection] ADD [InspectionType] NVARCHAR(50) NULL;
END
GO

-- Add Sample Information
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'QualityInspection') AND name = 'SampleLot')
BEGIN
    ALTER TABLE [QualityInspection] ADD [SampleLot] NVARCHAR(100) NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'QualityInspection') AND name = 'SampleSize')
BEGIN
    ALTER TABLE [QualityInspection] ADD [SampleSize] INT NULL;
END
GO

-- Add Dairy Test Parameters
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'QualityInspection') AND name = 'Temperature')
BEGIN
    ALTER TABLE [QualityInspection] ADD [Temperature] DECIMAL(5,2) NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'QualityInspection') AND name = 'PHLevel')
BEGIN
    ALTER TABLE [QualityInspection] ADD [PHLevel] DECIMAL(4,2) NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'QualityInspection') AND name = 'FatContent')
BEGIN
    ALTER TABLE [QualityInspection] ADD [FatContent] DECIMAL(5,2) NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'QualityInspection') AND name = 'ProteinContent')
BEGIN
    ALTER TABLE [QualityInspection] ADD [ProteinContent] DECIMAL(5,2) NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'QualityInspection') AND name = 'MoistureContent')
BEGIN
    ALTER TABLE [QualityInspection] ADD [MoistureContent] DECIMAL(5,2) NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'QualityInspection') AND name = 'Acidity')
BEGIN
    ALTER TABLE [QualityInspection] ADD [Acidity] DECIMAL(5,2) NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'QualityInspection') AND name = 'BacterialCount')
BEGIN
    ALTER TABLE [QualityInspection] ADD [BacterialCount] INT NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'QualityInspection') AND name = 'SomaticCellCount')
BEGIN
    ALTER TABLE [QualityInspection] ADD [SomaticCellCount] INT NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'QualityInspection') AND name = 'AntibioticTest')
BEGIN
    ALTER TABLE [QualityInspection] ADD [AntibioticTest] BIT NULL;
END
GO

-- Add Acceptable Ranges (JSON or simple string)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'QualityInspection') AND name = 'AcceptableRanges')
BEGIN
    ALTER TABLE [QualityInspection] ADD [AcceptableRanges] NVARCHAR(500) NULL;
END
GO

-- Add Dates
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'QualityInspection') AND name = 'InspectionDate')
BEGIN
    ALTER TABLE [QualityInspection] ADD [InspectionDate] DATETIME2 NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'QualityInspection') AND name = 'CompletedDate')
BEGIN
    ALTER TABLE [QualityInspection] ADD [CompletedDate] DATETIME2 NULL;
END
GO

-- Add Notes and Corrective Action
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'QualityInspection') AND name = 'Notes')
BEGIN
    ALTER TABLE [QualityInspection] ADD [Notes] NVARCHAR(2000) NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'QualityInspection') AND name = 'CorrectiveAction')
BEGIN
    ALTER TABLE [QualityInspection] ADD [CorrectiveAction] NVARCHAR(500) NULL;
END
GO

-- =============================================
-- PART 2: NonConformance Table - Create New Table
-- =============================================

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'NonConformance') AND type = 'U')
BEGIN
    CREATE TABLE [NonConformance] (
        [NonConformanceID] INT IDENTITY(1,1) NOT NULL,
        [CompanyID] INT NOT NULL,
        [QualityInspectionID] INT NULL,
        [ProductionBatchID] INT NULL,
        [ReportedByUserId] NVARCHAR(450) NOT NULL,
        [AssignedToUserId] NVARCHAR(450) NULL,
        [NCRNumber] NVARCHAR(50) NOT NULL,
        [Category] NVARCHAR(50) NOT NULL,
        [Severity] NVARCHAR(20) NOT NULL,
        [Title] NVARCHAR(200) NOT NULL,
        [Description] NVARCHAR(2000) NULL,
        [RootCause] NVARCHAR(2000) NULL,
        [ImmediateAction] NVARCHAR(2000) NULL,
        [CorrectiveAction] NVARCHAR(2000) NULL,
        [PreventiveAction] NVARCHAR(2000) NULL,
        [Status] NVARCHAR(50) NOT NULL,
        [AffectedQuantity] INT NULL,
        [CostImpact] DECIMAL(18,2) NULL,
        [Disposition] NVARCHAR(50) NULL,
        [ReportedDate] DATETIME2 NOT NULL,
        [DueDate] DATETIME2 NULL,
        [ClosedDate] DATETIME2 NULL,
        [VerifiedDate] DATETIME2 NULL,
        [VerifiedByUserId] NVARCHAR(450) NULL,
        
        CONSTRAINT [PK_NonConformance] PRIMARY KEY CLUSTERED ([NonConformanceID] ASC),
        CONSTRAINT [FK_NonConformance_Company_CompanyID] FOREIGN KEY ([CompanyID]) REFERENCES [Company]([CompanyID]) ON DELETE NO ACTION,
        CONSTRAINT [FK_NonConformance_QualityInspection_QualityInspectionID] FOREIGN KEY ([QualityInspectionID]) REFERENCES [QualityInspection]([QualityInspectionID]) ON DELETE SET NULL,
        CONSTRAINT [FK_NonConformance_ProductionBatch_ProductionBatchID] FOREIGN KEY ([ProductionBatchID]) REFERENCES [ProductionBatch]([ProductionBatchID]) ON DELETE SET NULL,
        CONSTRAINT [FK_NonConformance_AspNetUsers_ReportedByUserId] FOREIGN KEY ([ReportedByUserId]) REFERENCES [AspNetUsers]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_NonConformance_AspNetUsers_AssignedToUserId] FOREIGN KEY ([AssignedToUserId]) REFERENCES [AspNetUsers]([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_NonConformance_AspNetUsers_VerifiedByUserId] FOREIGN KEY ([VerifiedByUserId]) REFERENCES [AspNetUsers]([Id]) ON DELETE NO ACTION
    );
END
GO

-- Create Indexes for NonConformance
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_NonConformance_CompanyID' AND object_id = OBJECT_ID('NonConformance'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_NonConformance_CompanyID] ON [NonConformance]([CompanyID]);
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_NonConformance_QualityInspectionID' AND object_id = OBJECT_ID('NonConformance'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_NonConformance_QualityInspectionID] ON [NonConformance]([QualityInspectionID]);
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_NonConformance_ProductionBatchID' AND object_id = OBJECT_ID('NonConformance'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_NonConformance_ProductionBatchID] ON [NonConformance]([ProductionBatchID]);
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_NonConformance_ReportedByUserId' AND object_id = OBJECT_ID('NonConformance'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_NonConformance_ReportedByUserId] ON [NonConformance]([ReportedByUserId]);
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_NonConformance_AssignedToUserId' AND object_id = OBJECT_ID('NonConformance'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_NonConformance_AssignedToUserId] ON [NonConformance]([AssignedToUserId]);
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_NonConformance_VerifiedByUserId' AND object_id = OBJECT_ID('NonConformance'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_NonConformance_VerifiedByUserId] ON [NonConformance]([VerifiedByUserId]);
END
GO

-- =============================================
-- PART 3: ProductFormulation Table - Create New Table
-- =============================================

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'ProductFormulation') AND type = 'U')
BEGIN
    CREATE TABLE [ProductFormulation] (
        [FormulationID] INT IDENTITY(1,1) NOT NULL,
        [ProductID] INT NOT NULL,
        [CompanyID] INT NOT NULL,
        [RawMaterialID] INT NOT NULL,
        [Quantity] DECIMAL(18,4) NOT NULL,
        [Unit] NVARCHAR(50) NULL,
        [ProcessOrder] INT NULL,
        [ProcessInstructions] NVARCHAR(500) NULL,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt] DATETIME2 NULL,
        
        CONSTRAINT [PK_ProductFormulation] PRIMARY KEY CLUSTERED ([FormulationID] ASC),
        CONSTRAINT [FK_ProductFormulation_Product_ProductID] FOREIGN KEY ([ProductID]) REFERENCES [Product]([ProductID]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ProductFormulation_Company_CompanyID] FOREIGN KEY ([CompanyID]) REFERENCES [Company]([CompanyID]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ProductFormulation_RawMaterial_RawMaterialID] FOREIGN KEY ([RawMaterialID]) REFERENCES [RawMaterial]([RawMaterialID]) ON DELETE NO ACTION
    );
END
GO

-- Create Indexes for ProductFormulation
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ProductFormulation_ProductID' AND object_id = OBJECT_ID('ProductFormulation'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_ProductFormulation_ProductID] ON [ProductFormulation]([ProductID]);
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ProductFormulation_CompanyID' AND object_id = OBJECT_ID('ProductFormulation'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_ProductFormulation_CompanyID] ON [ProductFormulation]([CompanyID]);
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ProductFormulation_RawMaterialID' AND object_id = OBJECT_ID('ProductFormulation'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_ProductFormulation_RawMaterialID] ON [ProductFormulation]([RawMaterialID]);
END
GO

-- =============================================
-- PART 4: BillingInvoice - Add InvoiceDate Column
-- =============================================

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'BillingInvoice') AND name = 'InvoiceDate')
BEGIN
    ALTER TABLE [BillingInvoice] ADD [InvoiceDate] DATETIME2 NULL;
END
GO

-- =============================================
-- PART 5: Set Default Values for New Status Types
-- =============================================

-- Update existing QualityInspection records to set InspectionType if null
UPDATE [QualityInspection]
SET [InspectionType] = 'FinalProduct'
WHERE [InspectionType] IS NULL;
GO

-- =============================================
-- Script Complete
-- =============================================
PRINT 'DairyFlow QM Enhancements applied successfully.';
GO
