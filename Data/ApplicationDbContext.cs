using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using IT15_DairyFlow.Models;

namespace IT15_DairyFlow.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Core Business Entities
        public DbSet<Company> Company { get; set; }
        public DbSet<Subscription> Subscription { get; set; }
        public DbSet<Product> Product { get; set; }
        public DbSet<Inventory> Inventory { get; set; }
        public DbSet<Expense> Expense { get; set; }
        public DbSet<Supplier> Supplier { get; set; }

        // Production Entities
        public DbSet<RawMaterial> RawMaterial { get; set; }
        public DbSet<ProductionBatch> ProductionBatch { get; set; }
        public DbSet<ProductionCost> ProductionCost { get; set; }
        public DbSet<QualityInspection> QualityInspection { get; set; }
        public DbSet<NonConformance> NonConformance { get; set; }
        public DbSet<Equipment> Equipment { get; set; }

        // Financial Entities
        public DbSet<Budget> Budget { get; set; }
        public DbSet<BillingInvoice> BillingInvoice { get; set; }
        public DbSet<JournalEntry> JournalEntry { get; set; }

        // Product Formulation
        public DbSet<ProductFormulation> ProductFormulation { get; set; }

        // Audit
        public DbSet<AuditLog> AuditLog { get; set; }

        // Notifications
        public DbSet<Notification> Notification { get; set; }

        // Sales
        public DbSet<Sale> Sale { get; set; }
        public DbSet<SaleTransaction> SaleTransaction { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Company relationships
            modelBuilder.Entity<Company>()
                .HasOne(c => c.Subscription)
                .WithMany(s => s.Companies)
                .HasForeignKey(c => c.SubscriptionID)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ApplicationUser>()
                .HasOne(u => u.Company)
                .WithMany(c => c.Users)
                .HasForeignKey(u => u.CompanyID)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure Product relationships
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Company)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CompanyID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Product>()
                .HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserID)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Inventory relationships
            modelBuilder.Entity<Inventory>()
                .HasOne(i => i.Product)
                .WithMany(p => p.Inventories)
                .HasForeignKey(i => i.ProductID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Inventory>()
                .HasOne(i => i.Company)
                .WithMany(c => c.Inventories)
                .HasForeignKey(i => i.CompanyID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Inventory>()
                .HasOne(i => i.User)
                .WithMany()
                .HasForeignKey(i => i.UserID)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Expense relationships
            modelBuilder.Entity<Expense>()
                .HasOne(e => e.Company)
                .WithMany(c => c.Expenses)
                .HasForeignKey(e => e.CompanyID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Expense>()
                .HasOne(e => e.Supplier)
                .WithMany(s => s.Expenses)
                .HasForeignKey(e => e.SupplierID)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Expense>()
                .HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserID)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Supplier relationships
            modelBuilder.Entity<Supplier>()
                .HasOne(s => s.Company)
                .WithMany(c => c.Suppliers)
                .HasForeignKey(s => s.CompanyID)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure RawMaterial relationships
            modelBuilder.Entity<RawMaterial>()
                .HasOne(r => r.Supplier)
                .WithMany(s => s.RawMaterials)
                .HasForeignKey(r => r.SupplierID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RawMaterial>()
                .HasOne(r => r.Company)
                .WithMany(c => c.RawMaterials)
                .HasForeignKey(r => r.CompanyID)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure ProductionBatch relationships
            modelBuilder.Entity<ProductionBatch>()
                .HasOne(pb => pb.Company)
                .WithMany(c => c.ProductionBatches)
                .HasForeignKey(pb => pb.CompanyID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProductionBatch>()
                .HasOne(pb => pb.Product)
                .WithMany()
                .HasForeignKey(pb => pb.ProductID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProductionBatch>()
                .HasOne(pb => pb.Equipment)
                .WithMany(e => e.ProductionBatches)
                .HasForeignKey(pb => pb.EquipmentID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProductionBatch>()
                .HasOne(pb => pb.User)
                .WithMany()
                .HasForeignKey(pb => pb.UserID)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Equipment relationships
            modelBuilder.Entity<Equipment>()
                .HasOne(e => e.Company)
                .WithMany(c => c.Equipments)
                .HasForeignKey(e => e.CompanyID)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure ProductionCost relationships
            modelBuilder.Entity<ProductionCost>()
                .HasOne(pc => pc.ProductionBatch)
                .WithMany(pb => pb.ProductionCosts)
                .HasForeignKey(pc => pc.ProductionBatchID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProductionCost>()
                .HasOne(pc => pc.Company)
                .WithMany(c => c.ProductionCosts)
                .HasForeignKey(pc => pc.CompanyID)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure QualityInspection relationships
            modelBuilder.Entity<QualityInspection>()
                .HasOne(qi => qi.Company)
                .WithMany(c => c.QualityInspection)
                .HasForeignKey(qi => qi.CompanyID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<QualityInspection>()
                .HasOne(qi => qi.ProductionBatch)
                .WithMany(pb => pb.QualityInspection)
                .HasForeignKey(qi => qi.ProductionBatchID)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Budget relationships
            modelBuilder.Entity<Budget>()
                .HasOne(b => b.Company)
                .WithMany(c => c.Budgets)
                .HasForeignKey(b => b.CompanyID)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure BillingInvoice relationships
            modelBuilder.Entity<BillingInvoice>()
                .HasOne(bi => bi.Company)
                .WithMany(c => c.BillingInvoices)
                .HasForeignKey(bi => bi.CompanyID)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure JournalEntry relationships
            modelBuilder.Entity<JournalEntry>()
                .HasOne(je => je.Company)
                .WithMany(c => c.JournalEntries)
                .HasForeignKey(je => je.CompanyID)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure AuditLog relationships
            modelBuilder.Entity<AuditLog>()
                .HasOne(al => al.Company)
                .WithMany(c => c.AuditLogs)
                .HasForeignKey(al => al.CompanyID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AuditLog>()
                .HasOne(al => al.User)
                .WithMany()
                .HasForeignKey(al => al.UserID)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Notification relationships
            modelBuilder.Entity<Notification>()
                .HasOne(n => n.Recipient)
                .WithMany()
                .HasForeignKey(n => n.RecipientUserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Notification>()
                .HasOne(n => n.Actor)
                .WithMany()
                .HasForeignKey(n => n.ActorUserId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Notification>()
                .HasIndex(n => new { n.RecipientUserId, n.IsRead, n.CreatedAt })
                .HasDatabaseName("IX_Notification_Recipient_Read_Date");

            // Configure ProductFormulation relationships
            modelBuilder.Entity<ProductFormulation>()
                .HasOne(pf => pf.Product)
                .WithMany()
                .HasForeignKey(pf => pf.ProductID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProductFormulation>()
                .HasOne(pf => pf.Company)
                .WithMany()
                .HasForeignKey(pf => pf.CompanyID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProductFormulation>()
                .HasOne(pf => pf.RawMaterial)
                .WithMany()
                .HasForeignKey(pf => pf.RawMaterialID)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure NonConformance relationships
            modelBuilder.Entity<NonConformance>()
                .HasOne(nc => nc.Company)
                .WithMany(c => c.NonConformances)
                .HasForeignKey(nc => nc.CompanyID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<NonConformance>()
                .HasOne(nc => nc.QualityInspection)
                .WithMany(q => q.NonConformances)
                .HasForeignKey(nc => nc.QualityInspectionID)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<NonConformance>()
                .HasOne(nc => nc.ProductionBatch)
                .WithMany()
                .HasForeignKey(nc => nc.ProductionBatchID)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<NonConformance>()
                .HasOne(nc => nc.ReportedByUser)
                .WithMany()
                .HasForeignKey(nc => nc.ReportedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<NonConformance>()
                .HasOne(nc => nc.AssignedToUser)
                .WithMany()
                .HasForeignKey(nc => nc.AssignedToUserId)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure decimal precision for QualityInspection
            modelBuilder.Entity<QualityInspection>()
                .Property(q => q.Temperature).HasPrecision(5, 2);
            modelBuilder.Entity<QualityInspection>()
                .Property(q => q.PHLevel).HasPrecision(4, 2);
            modelBuilder.Entity<QualityInspection>()
                .Property(q => q.FatContent).HasPrecision(5, 2);
            modelBuilder.Entity<QualityInspection>()
                .Property(q => q.ProteinContent).HasPrecision(5, 2);
            modelBuilder.Entity<QualityInspection>()
                .Property(q => q.MoistureContent).HasPrecision(5, 2);
            modelBuilder.Entity<QualityInspection>()
                .Property(q => q.Acidity).HasPrecision(5, 2);

            // Configure decimal precision for NonConformance
            modelBuilder.Entity<NonConformance>()
                .Property(nc => nc.CostImpact).HasPrecision(18, 2);

            // Configure Sale relationships
            modelBuilder.Entity<Sale>()
                .HasOne(s => s.Company)
                .WithMany(c => c.Sales)
                .HasForeignKey(s => s.CompanyID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Sale>()
                .HasOne(s => s.Inventory)
                .WithMany()
                .HasForeignKey(s => s.InventoryID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Sale>()
                .HasOne(s => s.Product)
                .WithMany()
                .HasForeignKey(s => s.ProductID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Sale>()
                .HasOne(s => s.CreatedByUser)
                .WithMany()
                .HasForeignKey(s => s.CreatedByUserID)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure SaleTransaction relationships
            modelBuilder.Entity<SaleTransaction>()
                .HasOne(t => t.Sale)
                .WithMany(s => s.Transactions)
                .HasForeignKey(t => t.SaleID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SaleTransaction>()
                .HasOne(t => t.Company)
                .WithMany()
                .HasForeignKey(t => t.CompanyID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SaleTransaction>()
                .HasOne(t => t.ProcessedByUser)
                .WithMany()
                .HasForeignKey(t => t.ProcessedByUserID)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
