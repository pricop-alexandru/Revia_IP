using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Revia.Data;
using Revia.Models;
using System.Threading.Tasks;

namespace Revia.Pages.Locations
{
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DetailsModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public Location Location { get; set; } = null!;

        // Proprietăți pentru controlul afișării butonului
        public bool IsAdmin { get; set; }
        public bool IsOwnerOfLocation { get; set; }
        public string? CurrentUserId { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            Location = await _context.Locations
                .Include(l => l.Owner)
                .Include(l => l.Reviews)
                    .ThenInclude(r => r.User)
                .FirstOrDefaultAsync(m => m.Id == id && m.Status == "Approved");

            if (Location == null || Location.Status != "Approved") return NotFound();

            // Determinăm rolurile și proprietatea
            IsAdmin = User.IsInRole(UserRoles.Admin);

            CurrentUserId = _userManager.GetUserId(User);

            if (CurrentUserId != null && !IsAdmin && User.IsInRole(UserRoles.Owner))
            {
                var currentUser = await _userManager.GetUserAsync(User);
                var owner = await _context.Owners
                    .FirstOrDefaultAsync(o => o.ApplicationUserId == currentUser.Id);

                IsOwnerOfLocation = owner != null && Location.OwnerId == owner.Id;
            }
            else
            {
                IsOwnerOfLocation = false;
            }

            return Page();
        }

        public async Task<IActionResult> OnPostDeleteReviewAsync(int id, int reviewId)
        {
            var review = await _context.Reviews.FindAsync(reviewId);

            if (review == null) return NotFound();

            // Verifică dacă user-ul curent este owner-ul recenziei
            var currentUser = await _userManager.GetUserAsync(User);
            if (review.UserId != currentUser?.Id)
            {
                return Forbid();  // Interzis accesul dacă nu e owner
            }

            // Ștergere hard (fizică) din DB
            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();

            // Redirect înapoi la detaliile locației
            return RedirectToPage("./Details", new { id = id });
        }
    }
}