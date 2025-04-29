using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Coil.Api.Entities
{
    public class Sale : AuditableEntity
    {
        [Key]
        public int SaleId { get; set; }

        [Required]
        public int PlantId { get; set; }

        [ForeignKey("PlantId")]
        public Plant Plant { get; set; } = null!;

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Weight must be a positive value.")]
        public double Weight { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime SaleDate { get; set; }

        [Required]
        public string RawMaterialsJson { get; set; } = null!; // JSON string to store raw material IDs and sale percentages
    }
}
