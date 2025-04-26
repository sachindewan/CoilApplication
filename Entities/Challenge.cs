using System.ComponentModel.DataAnnotations;

namespace Coil.Api.Entities
{
    public class Challenge
    {
        [Key]
        public int ChallengeId { get; set; }

        [MaxLength(1000, ErrorMessage = "Challenge Name exceeds 1000 characters")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Challenge Name is required.")]
        public required string ChallengeName { get; set; } = null!;
    }
}
