using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace Coil.Api.Entities
{
    public class ApplicationUser : IdentityUser
    {
        [ForeignKey(nameof(Plant))]
        public int PlantId { get; set; }
    }
}
