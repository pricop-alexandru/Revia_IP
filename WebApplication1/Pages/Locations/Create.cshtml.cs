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
        public LocationInputModel Input { get; set; }

        public class LocationInputModel
        {
            [Required(ErrorMessage = "Numele este obligatoriu.")]
            public string Name { get; set; }

            public string Description { get; set; }

            [Required(ErrorMessage = "Adresa este obligatorie.")]
            public string Address { get; set; }

            public string? ImageUrl { get; set; }
        }

        public IActionResult OnGet()
        {
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                // ModelState invalid → rămâne pe pagină și arată erorile
                return Page();
            }

            var user = await _userManager.GetUserAsync(User);
            var owner = await _context.Owners
                .FirstOrDefaultAsync(o => o.ApplicationUserId == user.Id);

            if (owner == null)
            {
                TempData["ErrorMessage"] = "Nu ești înregistrat ca Owner.";
                return RedirectToPage("/Index");
            }

            // Creăm manual Location fără bind direct la entitate
            var location = new Location
            {
                Name = Input.Name,
                Description = Input.Description,
                Address = Input.Address,
                ImageUrl = Input.ImageUrl,
                OwnerId = owner.Id,
                Status = "Pending"  // Automat pending
            };

            _context.Locations.Add(location);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Localul a fost adăugat și este în așteptarea aprobării adminului.";
            return RedirectToPage("/Locations/Index");
        }
    }
}