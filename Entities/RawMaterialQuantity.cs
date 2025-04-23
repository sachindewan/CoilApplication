using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Coil.Api.Entities
{
    public class RawMaterialQuantity : AuditableEntity
    {
        public int Id { get; set; }

        [ForeignKey(nameof(RawMaterial))]
        public int RawMaterialId { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Available quantity must be a non-negative value.")]
        public decimal AvailableQuantity { get; set; }
        public RawMaterial RawMaterial { get; set; } = null!;
    }
}
