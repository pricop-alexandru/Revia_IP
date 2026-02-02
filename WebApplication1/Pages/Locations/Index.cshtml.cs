using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Revia.Data;
using Revia.Models;

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

        public List<LocationViewModel> Locations { get; set; } = new();
        public List<string> Cities { get; set; } = new(); // Pentru dropdown-ul de filtrare

        public class LocationViewModel
        {
            public Location Location { get; set; } = null!;
            public bool CanDelete { get; set; }
        }

        public async Task OnGetAsync(string sortOrder, string searchCity, string searchCategory)
        {
            // 1. Salvăm starea filtrelor pentru UI (pentru a rămâne selectate în dropdown/butoane)
            ViewData["CurrentSort"] = sortOrder;
            ViewData["CurrentCity"] = searchCity;
            ViewData["CurrentCategory"] = searchCategory;
            ViewData["RatingSortParm"] = sortOrder == "Rating" ? "rating_asc" : "Rating";

            // 2. Extragem lista de orașe unice pentru dropdown (doar locațiile aprobate)
            Cities = await _context.Locations
                .Where(l => l.Status == "Approved")
                .Select(l => l.City)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            // 3. Construim Query-ul de bază
            var query = _context.Locations
                .Include(l => l.Reviews)
                .Include(l => l.Owner)
                .Where(l => l.Status == "Approved")
                .AsQueryable();

            // 4. Aplicăm FILTRAREA după Oraș
            if (!string.IsNullOrEmpty(searchCity))
            {
                query = query.Where(l => l.City == searchCity);
            }

            // 5. Aplicăm FILTRAREA după Categorie (Vibe Check)
            if (!string.IsNullOrEmpty(searchCategory))
            {
                query = query.Where(l => l.Category == searchCategory);
            }

            // Executăm query-ul spre baza de date
            var locationsList = await query.ToListAsync();

            // 6. SORTARE în memorie (AverageRating este [NotMapped], deci nu se poate sorta direct în SQL)
            switch (sortOrder)
            {
                case "Rating":
                    locationsList = locationsList.OrderByDescending(l => l.AverageRating).ToList();
                    break;
                case "rating_asc":
                    locationsList = locationsList.OrderBy(l => l.AverageRating).ToList();
                    break;
                default:
                    locationsList = locationsList.OrderBy(l => l.Name).ToList();
                    break;
            }

            // 7. Logica de permisiuni pentru Ștergere (Admin vs Owner)
            var isAdmin = User.IsInRole(UserRoles.Admin);
            int? currentOwnerId = null;

            if (!isAdmin && User.IsInRole(UserRoles.Owner))
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser != null)
                {
                    var owner = await _context.Owners
                        .FirstOrDefaultAsync(o => o.ApplicationUserId == currentUser.Id);
                    currentOwnerId = owner?.Id;
                }
            }

            // 8. Mapăm rezultatele în ViewModel-ul paginii
            Locations = locationsList.Select(loc => new LocationViewModel
            {
                Location = loc,
                CanDelete = isAdmin || (currentOwnerId.HasValue && loc.OwnerId == currentOwnerId.Value)
            }).ToList();
        }

        [Authorize(Roles = UserRoles.Admin)]
        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var location = await _context.Locations.FindAsync(id);
            if (location == null) return RedirectToPage();
            _context.Locations.Remove(location);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Local șters.";
            return RedirectToPage();
        }

        [Authorize(Roles = UserRoles.Owner)]
        public async Task<IActionResult> OnPostDeleteOwnerAsync(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var owner = await _context.Owners.FirstOrDefaultAsync(o => o.ApplicationUserId == currentUser.Id);

            // Verificăm dacă locația aparține acestui owner înainte de ștergere
            var location = await _context.Locations.FirstOrDefaultAsync(l => l.Id == id && l.OwnerId == owner.Id);

            if (location != null)
            {
                _context.Locations.Remove(location);
                // CORECȚIE AICI: adăugăm _context. în față
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Localul tău a fost șters.";
            }
            else
            {
                TempData["ErrorMessage"] = "Nu s-a putut șterge localul sau nu aveți permisiunea necesară.";
            }

            return RedirectToPage();
        }
    }
}