using Coil.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace Coil.Api.Database
{
    public class CoilApplicationDbContext(DbContextOptions<CoilApplicationDbContext> dbContextOptions) : DbContext(dbContextOptions)
    {
        public DbSet<Plant> Plants { get; set; } = null!;
        public DbSet<Party> Parties { get; set; } = null!;
        public DbSet<RawMaterial> RawMaterials { get; set; } = null!;
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }
        public DbSet<RawMaterialPurchase> RawMaterialPurchases { get; set; } = null!;
        public DbSet<RawMaterialQuantity> RawMaterialQuantities { get; set; } = null!;
        public DbSet<Expense> Expenses { get; set; } = null!;
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Challenge> Challenges { get; set; } = null!;
        public DbSet<ChallengesState> ChallengesStates { get; set; } = null!;
        public DbSet<Wastage> Wastages { get; set; }
        public DbSet<Sale> Sales { get; set; }

        public DbSet<Enquiry> Enquiries { get; set; } = null!;
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Product>()
                .HasMany(p => p.ProductImages)
                .WithOne() 
                .HasForeignKey(pi => pi.ProductId)
                .OnDelete(DeleteBehavior.Restrict); 

            base.OnModelCreating(modelBuilder);
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.Entity.CreatedOn = DateTime.UtcNow;
                        entry.Entity.CreatedBy = "TODO";
                        entry.Entity.LastModifiedOn = DateTime.UtcNow;
                        entry.Entity.LastModifiedBy = "TODO";
                        break;
                    case EntityState.Modified:
                        entry.Entity.LastModifiedOn = DateTime.UtcNow;
                        entry.Entity.LastModifiedBy = "TODO";
                        break;
                }

            }
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }
    }
}
