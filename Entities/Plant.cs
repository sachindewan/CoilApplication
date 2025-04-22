using System.ComponentModel.DataAnnotations;

namespace Coil.Api.Entities
{
    public class Plant
    {
        public int PlantId { get; set; }

        [MaxLength(1000, ErrorMessage = "Plant Name exceeds 1000 characters")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Plant name is required.")]
        public required string PlantName { get; set; }

        [Required(AllowEmptyStrings = false, ErrorMessage = "Location is required.")]
        public required string Location { get; set; }
        public List<Party> Parties { get; set; } = [];
    }
}
