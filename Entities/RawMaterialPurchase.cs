using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Coil.Api.Entities
{
    public class RawMaterialPurchase : AuditableEntity
    {
        [Key]
        public int PurchaseId { get; set; }

        [ForeignKey(nameof(Plant))]
        public int PlantId { get; set; }
        public Plant Plant { get; set; } = null!;

        [MaxLength(100, ErrorMessage = "Bill Number exceeds 100 characters")]
        public required string BillNumber { get; set; }

        public decimal Weight { get; set; }
        public decimal Rate { get; set; }
        public decimal BillValue { get; set; }
        public int GST { get; set; }
        public decimal TotalBillAmount { get; set; }

        public DateTime PurchaseDate { get; set; }

        [ForeignKey(nameof(RawMaterial))]
        public int RawMaterialId { get; set; }
        public RawMaterial RawMaterial { get; set; } = null!;

        [ForeignKey(nameof(Party))]
        public int PartyId { get; set; }
        public Party Party { get; set; } = null!;
    }
}
