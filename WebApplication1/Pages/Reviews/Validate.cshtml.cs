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
    [Authorize(Roles = "LocalGuide,Admin")]
    public class ValidateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly GamificationService _gamificationService; // Injectăm service-ul

        public ValidateModel(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            GamificationService gamificationService)
        {
            _context = context;
            _userManager = userManager;
            _gamificationService = gamificationService;
        }

        public IList<Review> PendingReviews { get; set; } = new List<Review>();

        public async Task OnGetAsync()
        {
            PendingReviews = await _context.Reviews
                .Include(r => r.User)
                .Include(r => r.Location)
                .Where(r => r.Status == "Pending")
                .OrderByDescending(r => r.Date)
                .ToListAsync();
        }

        // APROBARE RECENZIE
        public async Task<IActionResult> OnPostApproveAsync(int id)
        {
            var review = await _context.Reviews
                .Include(r => r.User)
                .Include(r => r.Location)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (review == null || review.Status != "Pending")
            {
                TempData["ErrorMessage"] = "Recenzia nu a fost găsită sau nu este în așteptare.";
                return RedirectToPage();
            }

            review.Status = "Approved";
            review.XPAwarded = 50;
            await _context.SaveChangesAsync();

            // XP pentru autorul recenziei
            await _gamificationService.AwardXPAsync(review.User, 50, "Recenzie aprobată");

            // XP pentru validator (doar dacă e LocalGuide)
            if (User.IsInRole(UserRoles.LocalGuide))
            {
                var validator = await _userManager.GetUserAsync(User);
                await _gamificationService.AwardXPAsync(validator, 50, "Validare recenzie (aprobare)");
            }
            await _gamificationService.CheckAndAwardCouponsAsync(review.User.Id, review.LocationId, true);

            var notifAuthor = new Notification
            {
                UserId = review.User.Id,
                Text = $"Recenzia ta la '{review.Location.Name}' a fost aprobată! Ai primit +{review.XPAwarded} XP.",
                Date = DateTime.Now,
                IsRead = false
            };
            _context.Notifications.Add(notifAuthor);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Recenzie aprobată! +50 XP acordat autorului și validatorului.";
            return RedirectToPage();
        }

        // RESPINGERE RECENZIE
        public async Task<IActionResult> OnPostRejectAsync(int id)
        {
            var review = await _context.Reviews
                .Include(r => r.User)
                .Include(r => r.Location)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (review == null || review.Status != "Pending")
            {
                TempData["ErrorMessage"] = "Recenzia nu a fost găsită sau nu este în așteptare.";
                return RedirectToPage();
            }

            review.Status = "Rejected";
            review.XPAwarded = 0;

            await _context.SaveChangesAsync(); // Extra save pentru a actualiza statusul înainte de a acorda XP si notificari
            // XP doar pentru validator (dacă e LocalGuide)
            if (User.IsInRole(UserRoles.LocalGuide))
            {
                var validator = await _userManager.GetUserAsync(User);
                await _gamificationService.AwardXPAsync(validator, 50, "Validare recenzie (respingere)");
            }

            await _gamificationService.CheckAndAwardCouponsAsync(review.User.Id, review.LocationId, true);
            // --- COD NOU: NOTIFICARE AUTOR ---
            var notifReject = new Notification
            {
                UserId = review.User.Id,
                Text = $"Recenzia ta la '{review.Location.Name}' a fost respinsă de un moderator.",
                Date = DateTime.Now,
                IsRead = false
            };
            _context.Notifications.Add(notifReject);
            // ---------------------------------
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Recenzie respinsă! +50 XP acordat validatorului.";
            return RedirectToPage();
        }
    }
}