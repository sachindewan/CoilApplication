using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Coil.Api.Entities
{
    public class Expense : AuditableEntity
    {
        [Key]
        public int ExpenseId { get; set; }

        [ForeignKey(nameof(Plant))]
        public int PlantId { get; set; }
        public Plant Plant { get; set; } = null!;

        [Required(AllowEmptyStrings = false, ErrorMessage = "Expense Type is required.")]
        public required string ExpenseType { get; set; }

        public string? BillNumber { get; set; }

        public decimal BillValue { get; set; }

        public int GST { get; set; }

        public decimal TotalBillAmount { get; set; }

        public DateTime ExpenseDate { get; set; }

        [ForeignKey(nameof(Party))]
        public int PartyId { get; set; }
        public Party Party { get; set; } = null!;
    }
}
