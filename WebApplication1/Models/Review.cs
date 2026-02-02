using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Revia.Models
{
    public class Review
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Text { get; set; } // Am redenumit din Content -> Text

        [Range(1, 5)]
        public int Rating { get; set; }

        public DateTime Date { get; set; } = DateTime.Now; // Am redenumit din CreatedAt -> Date

        // Noi proprietăți cerute de erorile tale
        public int XPAwarded { get; set; } = 0; // Câte puncte a primit userul pentru review
        public string Status { get; set; } = "Pending"; // Pending, Approved, Reject
        // RELAȚII
        [Required]
        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }
        public string? ImageUrl { get; set; }
        [Required]
        public int LocationId { get; set; }

        [ForeignKey("LocationId")]
        public virtual Location Location { get; set; }
    }
}