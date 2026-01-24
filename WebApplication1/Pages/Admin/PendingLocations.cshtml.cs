using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Revia.Data;
using Revia.Models;
using System.Threading.Tasks;

namespace Revia.Pages.Admin
{
    [Authorize(Roles = UserRoles.Admin)]
    public class PendingLocationsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public PendingLocationsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<Location> PendingLocations { get; set; }

        public async Task OnGetAsync()
        {
            PendingLocations = await _context.Locations
                .Include(l => l.Owner)
                .ThenInclude(o => o.User)
                .Where(l => l.Status == "Pending")
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostApproveAsync(int id)
        {
            // Modificăm query-ul pentru a include Owner-ul
            var location = await _context.Locations
                .Include(l => l.Owner)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (location != null)
            {
                location.Status = "Approved";

                // --- COD NOU: NOTIFICARE ---
                if (location.Owner != null)
                {
                    var notification = new Notification
                    {
                        UserId = location.Owner.ApplicationUserId, // ID-ul de login al Ownerului
                        Text = $"Vești bune! Localul tău '{location.Name}' a fost aprobat și este acum vizibil publicului.",
                        Date = DateTime.Now,
                        IsRead = false
                    };
                    _context.Notifications.Add(notification);
                }
                // ---------------------------

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Localul a fost aprobat.";
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRejectAsync(int id)
        {
            var location = await _context.Locations
                .Include(l => l.Owner) // Includem ownerul
                .Include(l => l.Reviews)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (location == null)
            {
                TempData["ErrorMessage"] = "Localul nu a fost găsit.";
                return RedirectToPage();
            }

            // --- COD NOU: NOTIFICARE (înainte de ștergere) ---
            if (location.Owner != null)
            {
                var notification = new Notification
                {
                    UserId = location.Owner.ApplicationUserId,
                    Text = $"Din păcate, localul tău '{location.Name}' a fost respins de un administrator.",
                    Date = DateTime.Now,
                    IsRead = false
                };
                _context.Notifications.Add(notification);
            }
            // ---------------------------

            _context.Locations.Remove(location);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Localul \"{location.Name}\" a fost respins și șters.";
            return RedirectToPage();
        }
    }
}