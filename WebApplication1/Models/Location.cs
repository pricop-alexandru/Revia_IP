using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace Revia.Models
{
    public class Location
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Name { get; set; } // Ex: Hotel Transilvania
        public string Description { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string Category { get; set; }
        public string? ImageUrl { get; set; }
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected - Nou!
                                                        // Relația cu Proprietarul (Owner)
                                                        // O locație aparține unui singur Owner
        public bool IsOfficialPartner { get; set; } = false;
        public int? OwnerId { get; set; }
        public virtual Owner Owner { get; set; }
        // O locație are multe recenzii
        public virtual ICollection<Review> Reviews { get; set; }
        public virtual ICollection<Coupon> Coupons { get; set; }
        [NotMapped]
        public double AverageRating
        {
            get
            {
                if (Reviews == null || !Reviews.Any(r => r.Status == "Approved"))
                    return 0;

                return Math.Round(Reviews.Where(r => r.Status == "Approved").Average(r => r.Rating), 1);
            }
        }

        [NotMapped]
        public int ReviewCount
        {
            get
            {
                return Reviews?.Count(r => r.Status == "Approved") ?? 0;
            }
        }
    }
}