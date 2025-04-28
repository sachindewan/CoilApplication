using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Coil.Api.Entities
{
    public class RawMaterialQuantity : AuditableEntity
    {
        public int Id { get; set; }

        [ForeignKey(nameof(Plant))]
        public int PlantId { get; set; }

        public Plant Plant { get; set; } = null!;

        [ForeignKey(nameof(RawMaterial))]
        public int RawMaterialId { get; set; }

        public RawMaterial RawMaterial { get; set; } = null!;

        [Range(0, double.MaxValue, ErrorMessage = "Available quantity must be a non-negative value.")]
        public decimal AvailableQuantity { get; set; }
    }
}
