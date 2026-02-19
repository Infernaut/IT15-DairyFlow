using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using IT15_DairyFlow.Models;

namespace IT15_DairyFlow.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Core Business Entities
        public DbSet<Company> Companies { get; set; }
        public DbSet<Subscription> Subscriptions { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Inventory> Inventories { get; set; }
        public DbSet<Expense> Expenses { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }

        // Production Entities
        public DbSet<RawMaterial> RawMaterials { get; set; }
        public DbSet<ProductionBatch> ProductionBatches { get; set; }
        public DbSet<ProductionCost> ProductionCosts { get; set; }
        public DbSet<QualityInspection> QualityInspections { get; set; }

        // Financial Entities
        public DbSet<Budget> Budgets { get; set; }
        public DbSet<BillingInvoice> BillingInvoices { get; set; }
        public DbSet<JournalEntry> JournalEntries { get; set; }

        // Audit
        public DbSet<AuditLog> AuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Company relationships
            modelBuilder.Entity<Company>()
                .HasOne(c => c.Subscription)
                .WithMany(s => s.Companies)
                .HasForeignKey(c => c.SubscriptionID)
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
                .WithMany(c => c.QualityInspections)
                .HasForeignKey(qi => qi.CompanyID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<QualityInspection>()
                .HasOne(qi => qi.ProductionBatch)
                .WithMany(pb => pb.QualityInspections)
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
        }
    }
}
