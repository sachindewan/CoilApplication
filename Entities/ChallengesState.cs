using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Coil.Api.Entities
{
    public class ChallengesState : AuditableEntity
    {
        [Key]
        public int ChallengesStateId { get; set; }

        [ForeignKey(nameof(Plant))]
        [Required(ErrorMessage = "Plant ID is required.")]
        public int PlantId { get; set; }
        public Plant Plant { get; set; } = null!;

        [ForeignKey(nameof(Challenge))]
        [Required(ErrorMessage = "Challenge ID is required.")]
        public int ChallengeId { get; set; }
        public Challenge Challenge { get; set; } = null!;

        [Required(ErrorMessage = "Challenge Start DateTime is required.")]
        public DateTime ChallengeStartDateTime { get; set; }

        [Required(AllowEmptyStrings = false, ErrorMessage = "Challenge State is required.")]
        public bool State { get; set; } = true;
    }
}
