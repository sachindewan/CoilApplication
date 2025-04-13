using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Coil.Api.Database
{
    public class CoilIdentityDbContext : IdentityDbContext<IdentityUser>
    {
        public CoilIdentityDbContext(DbContextOptions<CoilIdentityDbContext> dbContextOptions) : base(dbContextOptions)
        {
            
        }
    }
}
