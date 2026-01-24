using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Revia.Models
{
    public class OwnerRequest
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string CompanyName { get; set; } // Detalii furnizate în cerere, ex: numele companiei
        public string? TaxIdentificationNumber { get; set; } // CUI, opțional inițial
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected
        public DateTime RequestDate { get; set; } = DateTime.Now;
        // Relație cu userul care cere
        [Required]
        public string ApplicationUserId { get; set; }
        [ForeignKey("ApplicationUserId")]
        public virtual ApplicationUser User { get; set; }
    }
}