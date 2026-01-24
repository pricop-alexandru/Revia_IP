using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Revia.Models
{
    public class Owner
    {
        [Key]
        public int Id { get; set; }

        public string CompanyName { get; set; } // Ex: SC Pizzeria SRL
        public string? TaxIdentificationNumber { get; set; } // CUI
        public bool IsVerified { get; set; } = false;

        // --- RELAȚIA CU CONTUL DE LOGIN ---
        [Required]
        public string ApplicationUserId { get; set; }

        [ForeignKey("ApplicationUserId")]
        public virtual ApplicationUser User { get; set; }
    }
}