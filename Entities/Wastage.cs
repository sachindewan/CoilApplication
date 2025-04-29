using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Coil.Api.Entities
{
    public class Wastage : AuditableEntity
    {
        [Key]
        public int WastageId { get; set; }

        [Required]
        public int PlantId { get; set; }

        [ForeignKey("PlantId")]
        public Plant Plant { get; set; } = null!; 

        [Required]
        public int RawMaterialId { get; set; }

        [ForeignKey("RawMaterialId")]
        public RawMaterial RawMaterial { get; set; } = null!;

        [Required]
        [Range(0, 100)]
        public double WastagePercentage { get; set; }

        [Required]
        [MaxLength(500)]
        public required string WastageReason { get; set; }
    }
}
