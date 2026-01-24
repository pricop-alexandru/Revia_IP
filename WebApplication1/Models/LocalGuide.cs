using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Revia.Models
{
    public class LocalGuide
    {
        [Key]
        public int Id { get; set; }

        public string Region { get; set; } = string.Empty;

        [Required]
        public string ApplicationUserId { get; set; } = string.Empty;

        [ForeignKey("ApplicationUserId")]
        public virtual ApplicationUser User { get; set; } = null!;
    }
}