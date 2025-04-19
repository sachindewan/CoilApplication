using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Coil.Api.Entities
{
    public class Party
    {
        public int PartyId { get; set; }

        [MaxLength(1000, ErrorMessage = "Party Name exceeds 1000 characters")]
        public required string PartyName { get; set; }

        [ForeignKey(nameof(Plant))]
        public int PlantId { get; set; }
    }
}
