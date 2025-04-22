using System.ComponentModel.DataAnnotations;

namespace Coil.Api.Entities
{
    public class RawMaterial
    {
        [Key]
        public int RawMaterialId { get; set; }

        [MaxLength(1000, ErrorMessage = "Raw Material Name exceeds 1000 characters")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Raw material name is required.")]
        public required string RawMaterialName { get; set; }
    }
}
