using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Coil.Api.Entities
{
    public class Payment : AuditableEntity
    {
        [Key]
        public int PaymentId { get; set; }

        [ForeignKey(nameof(Plant))]
        public int PlantId { get; set; }
        public Plant Plant { get; set; } = null!;

        [ForeignKey(nameof(Party))]
        public int PartyId { get; set; }
        public Party Party { get; set; } = null!;

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
        public decimal Amount { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime PaymentDate { get; set; }
    }
}
