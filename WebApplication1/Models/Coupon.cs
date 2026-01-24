using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Revia.Models
{
    public class Coupon
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Title { get; set; } // Ex: "1+1 Gratis"

        public string Description { get; set; } // Detalii

        [Required]
        public DateTime ExpirationDate { get; set; }

        // Condiție: Minim X recenzii aprobate la acest local pentru a primi cuponul
        // Default 1 (chiar recenzia curentă)
        public int RequiredReviewsCount { get; set; } = 1;

        public bool IsActive { get; set; } = true;

        [Required]
        public int LocationId { get; set; }

        [ForeignKey("LocationId")]
        public virtual Location Location { get; set; }

        public virtual ICollection<UserCoupon> UserCoupons { get; set; }
    }
}