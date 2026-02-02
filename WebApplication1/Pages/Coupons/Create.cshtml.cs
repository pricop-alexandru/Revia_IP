using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Revia.Data;
using Revia.Models;
using System.ComponentModel.DataAnnotations;

namespace Revia.Pages.Coupons
{
    [Authorize(Roles = UserRoles.Owner)]
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CreateModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty]
        public CouponInputModel Input { get; set; }

        public string LocationName { get; set; }

        [BindProperty(SupportsGet = true)]
        public int LocationId { get; set; }

        public class CouponInputModel
        {
            [Required(ErrorMessage = "Titlul este obligatoriu.")]
            public string? Title { get; set; }

            public string? Description { get; set; } // Opțional (va fi null dacă e gol)

            public DateTime? ExpirationDate { get; set; } // Opțional

            public bool HasThreshold { get; set; }

            public int? ThresholdCount { get; set; } // Opțional
            public int RequiredLevel { get; set; } = 1;
        }

        public async Task<IActionResult> OnGetAsync(int locationId)
        {
            var user = await _userManager.GetUserAsync(User);
            var location = await _context.Locations.Include(l => l.Owner).FirstOrDefaultAsync(l => l.Id == locationId);

            // Securitate: Verificam daca e ownerul locatiei
            if (location == null || location.Owner.ApplicationUserId != user.Id)
            {
                return RedirectToPage("/Locations/Index");
            }

            LocationName = location.Name;
            LocationId = locationId;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Ștergem validările automate stricte pentru a gestiona noi valorile nule
            if (!ModelState.IsValid)
            {
                var loc = await _context.Locations.FindAsync(LocationId);
                LocationName = loc?.Name;
                return Page();
            }

            var coupon = new Coupon
            {
                LocationId = LocationId,
                Title = Input.Title,
                Description = Input.Description, // Rămâne null dacă e gol

                // Dacă data e null, punem Azi + 30 zile
                ExpirationDate = Input.ExpirationDate ?? DateTime.Now.AddDays(30),

                RequiredLevel = Input.RequiredLevel,
                RequiredReviewsCount = Input.HasThreshold ? (Input.ThresholdCount ?? 1) : 1,

                IsActive = true
            };

            _context.Coupons.Add(coupon);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Cuponul a fost creat!";
            return RedirectToPage("/Locations/Details", new { id = LocationId });
        }
    }
}