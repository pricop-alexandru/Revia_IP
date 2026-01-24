using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Revia.Data;
using Revia.Models;
using Revia.Services; // Adaugă asta
using System.Threading.Tasks;

namespace Revia.Pages.Reviews
{
    [Authorize]
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly GamificationService _gamificationService; // Injectăm service-ul

        public CreateModel(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            GamificationService gamificationService)
        {
            _context = context;
            _userManager = userManager;
            _gamificationService = gamificationService;
        }

        [BindProperty]
        public Review Review { get; set; } = default!;

        public string LocationName { get; set; } = "";

        public async Task<IActionResult> OnGetAsync(int locationId)
        {
            var location = await _context.Locations
                .Include(l => l.Owner)
                .FirstOrDefaultAsync(l => l.Id == locationId);

            if (location == null || location.Status != "Approved")
            {
                TempData["ErrorMessage"] = "Această locație nu există sau nu este aprobată pentru recenzii.";
                return RedirectToPage("/Locations/Index");
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var owner = await _context.Owners
                .FirstOrDefaultAsync(o => o.ApplicationUserId == currentUser.Id);

            if (owner != null && location.OwnerId == owner.Id)
            {
                TempData["ErrorMessage"] = "Nu poți scrie recenzii la propriile tale localuri.";
                return RedirectToPage("/Locations/Details", new { id = locationId });
            }

            LocationName = location.Name;
            Review = new Review { LocationId = locationId };
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var location = await _context.Locations
                .Include(l => l.Owner)
                .FirstOrDefaultAsync(l => l.Id == Review.LocationId);

            if (location == null || location.Status != "Approved")
            {
                TempData["ErrorMessage"] = "Această locație nu poate primi recenzii în momentul de față.";
                return RedirectToPage("/Locations/Index");
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var owner = await _context.Owners
                .FirstOrDefaultAsync(o => o.ApplicationUserId == currentUser.Id);

            if (owner != null && location.OwnerId == owner.Id)
            {
                TempData["ErrorMessage"] = "Nu poți scrie recenzii la propriile tale localuri.";
                return RedirectToPage("/Locations/Details", new { id = Review.LocationId });
            }

            ModelState.Remove("Review.User");
            ModelState.Remove("Review.Location");
            ModelState.Remove("Review.UserId");

            if (!ModelState.IsValid)
            {
                LocationName = location.Name;
                return Page();
            }

            Review.UserId = currentUser.Id;
            Review.Date = DateTime.Now;

            if (User.IsInRole(UserRoles.LocalGuide))
            {
                Review.Status = "Approved";
                Review.XPAwarded = 50;

                await _gamificationService.AwardXPAsync(currentUser, 50, "Recenzie scrisă (LocalGuide - aprobare automată)");

                TempData["SuccessMessage"] = "Recenzia ta a fost publicată automat (LocalGuide) și ai primit 50 XP!";
            }
            else
            {
                Review.Status = "Pending";
                Review.XPAwarded = 0;

                TempData["SuccessMessage"] = "Recenzia ta a fost trimisă și așteaptă validarea unui LocalGuide.";
            }

            _context.Reviews.Add(Review);
            await _context.SaveChangesAsync();

            return RedirectToPage("/Locations/Details", new { id = Review.LocationId });
        }
    }
}