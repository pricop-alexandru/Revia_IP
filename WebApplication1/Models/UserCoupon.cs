using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Revia.Models
{
    public class UserCoupon
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }

        [Required]
        public int CouponId { get; set; }
        [ForeignKey("CouponId")]
        public virtual Coupon Coupon { get; set; }

        public bool IsClaimed { get; set; } = false; // A fost folosit?
        public DateTime DateReceived { get; set; } = DateTime.Now;
        public DateTime? DateClaimed { get; set; }
    }
}