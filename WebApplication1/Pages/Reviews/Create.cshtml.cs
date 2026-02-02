using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Revia.Data;
using Revia.Models;
using Revia.Services;
using System.Threading.Tasks;
using System.IO; // Necesar pentru Path și Directory
using Microsoft.AspNetCore.Hosting; // Necesar pentru IWebHostEnvironment

namespace Revia.Pages.Reviews
{
    [Authorize]
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly GamificationService _gamificationService;
        private readonly IWebHostEnvironment _environment; // Pentru acces la folderul wwwroot

        public CreateModel(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            GamificationService gamificationService,
            IWebHostEnvironment environment)
        {
            _context = context;
            _userManager = userManager;
            _gamificationService = gamificationService;
            _environment = environment;
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

        public async Task<IActionResult> OnPostAsync(IFormFile? uploadedFile)
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

            // Eliminăm erorile de validare pentru obiectele de navigare
            ModelState.Remove("Review.User");
            ModelState.Remove("Review.Location");
            ModelState.Remove("Review.UserId");

            if (!ModelState.IsValid)
            {
                LocationName = location.Name;
                return Page();
            }

            // --- LOGICĂ UPLOAD IMAGINE ---
            if (uploadedFile != null && uploadedFile.Length > 0)
            {
                // Definim calea unde salvăm (wwwroot/uploads/reviews)
                string uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "reviews");

                // Creăm folderul dacă nu există
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Generăm un nume unic pentru fișier (GUID + extensie originală)
                string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(uploadedFile.FileName);
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // Salvăm fișierul pe disc
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await uploadedFile.CopyToAsync(fileStream);
                }

                // Salvăm calea relativă în baza de date
                Review.ImageUrl = "/uploads/reviews/" + uniqueFileName;
            }
            // -----------------------------

            Review.UserId = currentUser.Id;
            Review.Date = DateTime.Now;

            // Logica pentru Local Guide (Aprobare automată)
            if (User.IsInRole(UserRoles.LocalGuide))
            {
                Review.Status = "Approved";
                Review.XPAwarded = 50;

                _context.Reviews.Add(Review);
                await _context.SaveChangesAsync();

                await _gamificationService.AwardXPAsync(currentUser, 50, "Recenzie scrisă (LocalGuide - aprobare automată)");
                await _gamificationService.CheckAndAwardCouponsAsync(currentUser.Id, Review.LocationId, true);

                TempData["SuccessMessage"] = "Recenzia ta a fost publicată automat (LocalGuide) și ai primit 50 XP!";
            }
            else
            {
                // Logica pentru User normal (Așteaptă validare)
                Review.Status = "Pending";
                Review.XPAwarded = 0;

                _context.Reviews.Add(Review);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Recenzia ta a fost trimisă și așteaptă validarea unui LocalGuide.";
            }

            return RedirectToPage("/Locations/Details", new { id = Review.LocationId });
        }
    }
}