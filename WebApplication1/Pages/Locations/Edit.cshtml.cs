using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Revia.Data;
using Revia.Models;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace Revia.Pages.Locations
{
    [Authorize(Roles = UserRoles.Owner)]
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public EditModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty]
        public LocationInputModel Input { get; set; } = null!;

        public class LocationInputModel
        {
            [Required(ErrorMessage = "Numele este obligatoriu.")]
            public string Name { get; set; } = null!;

            public string? Description { get; set; }

            [Required(ErrorMessage = "Adresa este obligatorie.")]
            public string Address { get; set; } = null!;

            public string? ImageUrl { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            var location = await _context.Locations
                .Include(l => l.Owner)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (location == null) return NotFound();

            // Verificăm dacă user-ul curent este Owner-ul locației
            var currentUser = await _userManager.GetUserAsync(User);
            var owner = await _context.Owners
                .FirstOrDefaultAsync(o => o.ApplicationUserId == currentUser.Id);

            if (owner == null || location.OwnerId != owner.Id)
            {
                TempData["ErrorMessage"] = "Nu poți edita acest local – nu îți aparține.";
                return RedirectToPage("/Locations/Index");
            }

            Input = new LocationInputModel
            {
                Name = location.Name,
                Description = location.Description,
                Address = location.Address,
                ImageUrl = location.ImageUrl
            };

            ViewData["LocationId"] = location.Id;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            if (!ModelState.IsValid)
            {
                ViewData["LocationId"] = id;
                return Page();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var owner = await _context.Owners
                .FirstOrDefaultAsync(o => o.ApplicationUserId == currentUser.Id);

            var location = await _context.Locations
                .Include(l => l.Owner)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (location == null || owner == null || location.OwnerId != owner.Id)
            {
                TempData["ErrorMessage"] = "Nu poți edita acest local.";
                return RedirectToPage("/Locations/Index");
            }

            // Actualizăm câmpurile
            location.Name = Input.Name;
            location.Description = Input.Description;
            location.Address = Input.Address;
            location.ImageUrl = Input.ImageUrl;

            // Important: Editarea face locația Pending din nou
            location.Status = "Pending";

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Localul a fost actualizat și este în așteptarea re-aprobării de către admin.";
            return RedirectToPage("/Locations/Index");
        }
    }
}