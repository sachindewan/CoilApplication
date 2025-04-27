using Coil.Api.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Coil.Api.Database
{
    public class CoilIdentityDbContext : IdentityDbContext<ApplicationUser>
    {
        public CoilIdentityDbContext(DbContextOptions<CoilIdentityDbContext> dbContextOptions) : base(dbContextOptions)
        {
            
        }
    }
}
