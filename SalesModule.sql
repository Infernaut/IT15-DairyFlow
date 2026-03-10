-- =====================================================
-- DairyFlow: Sales & Transactions Module Migration
-- Creates Sale and SaleTransaction tables
-- =====================================================

-- Create Sale table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Sale')
BEGIN
    CREATE TABLE [dbo].[Sale] (
        [SaleID]            INT IDENTITY(1,1)   NOT NULL,
        [CompanyID]         INT                 NOT NULL,
        [InventoryID]       INT                 NOT NULL,
        [ProductID]         INT                 NOT NULL,
        [BuyerName]         NVARCHAR(256)       NOT NULL,
        [BuyerEmail]        NVARCHAR(256)       NULL,
        [BuyerPhone]        NVARCHAR(50)        NULL,
        [Quantity]          INT                 NOT NULL,
        [UnitPrice]         DECIMAL(18,2)       NOT NULL,
        [TotalAmount]       DECIMAL(18,2)       NOT NULL,
        [InvoiceNumber]     NVARCHAR(30)        NOT NULL,
        [SaleDate]          DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
        [PaymentStatus]     NVARCHAR(50)        NOT NULL DEFAULT 'Pending',
        [PaymentMethod]     NVARCHAR(50)        NULL,
        [CreatedByUserID]   NVARCHAR(450)       NOT NULL,
        [Notes]             NVARCHAR(500)       NULL,
        CONSTRAINT [PK_Sale] PRIMARY KEY CLUSTERED ([SaleID] ASC),
        CONSTRAINT [FK_Sale_Company] FOREIGN KEY ([CompanyID]) REFERENCES [dbo].[Company]([CompanyID]),
        CONSTRAINT [FK_Sale_Inventory] FOREIGN KEY ([InventoryID]) REFERENCES [dbo].[Inventory]([InventoryID]),
        CONSTRAINT [FK_Sale_Product] FOREIGN KEY ([ProductID]) REFERENCES [dbo].[Product]([ProductID]),
        CONSTRAINT [FK_Sale_CreatedByUser] FOREIGN KEY ([CreatedByUserID]) REFERENCES [dbo].[AspNetUsers]([Id])
    );

    -- Indexes for common queries
    CREATE NONCLUSTERED INDEX [IX_Sale_CompanyID] ON [dbo].[Sale]([CompanyID]);
    CREATE NONCLUSTERED INDEX [IX_Sale_PaymentStatus] ON [dbo].[Sale]([CompanyID], [PaymentStatus]);
    CREATE NONCLUSTERED INDEX [IX_Sale_InvoiceNumber] ON [dbo].[Sale]([InvoiceNumber]);

    PRINT 'Created Sale table.';
END
ELSE
    PRINT 'Sale table already exists.';
GO

-- Create SaleTransaction table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SaleTransaction')
BEGIN
    CREATE TABLE [dbo].[SaleTransaction] (
        [TransactionID]         INT IDENTITY(1,1)   NOT NULL,
        [SaleID]                INT                 NOT NULL,
        [CompanyID]             INT                 NOT NULL,
        [Amount]                DECIMAL(18,2)       NOT NULL,
        [PaymentMethod]         NVARCHAR(50)        NOT NULL,
        [PaymentDate]           DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
        [ProcessedByUserID]     NVARCHAR(450)       NOT NULL,
        [PayMongoSessionId]     NVARCHAR(256)       NULL,
        [ReferenceNumber]       NVARCHAR(100)       NULL,
        [Notes]                 NVARCHAR(500)       NULL,
        CONSTRAINT [PK_SaleTransaction] PRIMARY KEY CLUSTERED ([TransactionID] ASC),
        CONSTRAINT [FK_SaleTransaction_Sale] FOREIGN KEY ([SaleID]) REFERENCES [dbo].[Sale]([SaleID]),
        CONSTRAINT [FK_SaleTransaction_Company] FOREIGN KEY ([CompanyID]) REFERENCES [dbo].[Company]([CompanyID]),
        CONSTRAINT [FK_SaleTransaction_ProcessedByUser] FOREIGN KEY ([ProcessedByUserID]) REFERENCES [dbo].[AspNetUsers]([Id])
    );

    -- Indexes
    CREATE NONCLUSTERED INDEX [IX_SaleTransaction_SaleID] ON [dbo].[SaleTransaction]([SaleID]);
    CREATE NONCLUSTERED INDEX [IX_SaleTransaction_CompanyID] ON [dbo].[SaleTransaction]([CompanyID]);

    PRINT 'Created SaleTransaction table.';
END
ELSE
    PRINT 'SaleTransaction table already exists.';
GO
