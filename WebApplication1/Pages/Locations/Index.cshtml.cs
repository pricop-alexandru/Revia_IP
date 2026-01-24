using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Revia.Data;
using Revia.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Revia.Pages.Locations
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public IndexModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Schimbăm tipul la un ViewModel care conține și informația despre proprietate
        public List<LocationViewModel> Locations { get; set; } = new();

        public class LocationViewModel
        {
            public Location Location { get; set; } = null!;
            public bool CanDelete { get; set; } // true dacă Admin sau Owner-ul locației
        }

        public async Task OnGetAsync(string sortOrder)
        {
            ViewData["CurrentSort"] = sortOrder;
            ViewData["RatingSortParm"] = sortOrder == "Rating" ? "rating_asc" : "Rating";

            var locationsQuery = await _context.Locations
                .Include(l => l.Reviews)
                .Include(l => l.Owner)
                .Where(l => l.Status == "Approved")
                .ToListAsync(); // Aducem în memorie pentru a folosi proprietatea [NotMapped]

            // Sortare Logică
            switch (sortOrder)
            {
                case "Rating": // Descrescător (default logic pentru top)
                    locationsQuery = locationsQuery.OrderByDescending(l => l.AverageRating).ToList();
                    break;
                case "rating_asc":
                    locationsQuery = locationsQuery.OrderBy(l => l.AverageRating).ToList();
                    break;
                default: // Sortare după nume implicit
                    locationsQuery = locationsQuery.OrderBy(l => l.Name).ToList();
                    break;
            }
            var approvedLocations = await _context.Locations
                .Include(l => l.Reviews)
                .Include(l => l.Owner)
                .Where(l => l.Status == "Approved")
                .OrderBy(l => l.Name)
                .ToListAsync();

            var isAdmin = User.IsInRole(UserRoles.Admin);
            int? currentOwnerId = null;

            if (!isAdmin && User.IsInRole(UserRoles.Owner))
            {
                var currentUser = await _userManager.GetUserAsync(User);
                var owner = await _context.Owners
                    .FirstOrDefaultAsync(o => o.ApplicationUserId == currentUser.Id);

                currentOwnerId = owner?.Id;
            }
            foreach (var loc in approvedLocations)
            {
                bool canDelete = isAdmin || (currentOwnerId.HasValue && loc.OwnerId == currentOwnerId.Value);

                Locations.Add(new LocationViewModel
                {
                    Location = loc,
                    CanDelete = canDelete
                });
            }
            Locations.Clear();
            foreach (var loc in locationsQuery)
            {
                bool canDelete = isAdmin || (currentOwnerId.HasValue && loc.OwnerId == currentOwnerId.Value);
                Locations.Add(new LocationViewModel
                { 
                    Location = loc, 
                    CanDelete = canDelete 
                });
            }
        }

        // Ștergere pentru Admin
        [Authorize(Roles = UserRoles.Admin)]
        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var location = await _context.Locations
                .Include(l => l.Reviews)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (location == null)
            {
                TempData["ErrorMessage"] = "Localul nu a fost găsit.";
                return RedirectToPage();
            }

            _context.Locations.Remove(location);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Localul \"{location.Name}\" a fost șters definitiv.";
            return RedirectToPage();
        }

        // Ștergere pentru Owner (doar propriile localuri)
        [Authorize(Roles = UserRoles.Owner)]
        public async Task<IActionResult> OnPostDeleteOwnerAsync(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var owner = await _context.Owners
                .FirstOrDefaultAsync(o => o.ApplicationUserId == currentUser.Id);

            if (owner == null)
            {
                TempData["ErrorMessage"] = "Nu ești înregistrat ca Owner.";
                return RedirectToPage();
            }

            var location = await _context.Locations
                .Include(l => l.Reviews)
                .FirstOrDefaultAsync(l => l.Id == id && l.OwnerId == owner.Id);

            if (location == null)
            {
                TempData["ErrorMessage"] = "Nu poți șterge acest local – nu îți aparține.";
                return RedirectToPage();
            }

            _context.Locations.Remove(location);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Localul tău \"{location.Name}\" a fost șters definitiv.";
            return RedirectToPage();
        }
    }
}