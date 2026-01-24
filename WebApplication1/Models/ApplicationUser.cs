using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace Revia.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public DateTime DateJoined { get; set; } = DateTime.Now;

        // XP și Level pentru TOȚI utilizatorii (Client, Owner, LocalGuide)
        public int XP { get; set; } = 0;
        public int Level { get; set; } = 1;

        // Relații
        public virtual Owner? Owner { get; set; }
        public virtual LocalGuide? LocalGuide { get; set; }
        public virtual Admin? Admin { get; set; }
        public virtual ICollection<UserCoupon> UserCoupons { get; set; }
        public static int CalculateLevel(int xp)
        {
            int level = 1;
            int required = 500;

            while (xp >= required)
            {
                xp -= required;
                level++;
                required *= 2; // dublare
            }

            return level;
        }
    }
}