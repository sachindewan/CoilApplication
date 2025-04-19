using Coil.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace Coil.Api.Database
{
    public class CoilApplicationDbContext(DbContextOptions<CoilApplicationDbContext> dbContextOptions) : DbContext(dbContextOptions)
    {
        public DbSet<Plant> Plants { get; set; } = null!;
        public DbSet<Party> Parties { get; set; } = null!;
        public DbSet<RawMaterial> RawMaterials { get; set; } = null!;

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
