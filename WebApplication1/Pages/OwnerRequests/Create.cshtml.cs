using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Revia.Data;
using Revia.Models;
using System.Net.NetworkInformation;
using static System.Runtime.InteropServices.JavaScript.JSType;


namespace Revia.Pages.OwnerRequests
{
    [Authorize]
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<CreateModel> _logger;

        public CreateModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager, ILogger<CreateModel> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        [BindProperty]
        public OwnerRequestInputModel Input { get; set; }  // Folosim un ViewModel separat!

        public string UserFullName { get; set; }

        public class OwnerRequestInputModel
        {
            public string CompanyName { get; set; }

            public string? TaxIdentificationNumber { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            if (await _userManager.IsInRoleAsync(user, UserRoles.Owner) ||
                await _userManager.IsInRoleAsync(user, UserRoles.LocalGuide))
            {
                return RedirectToPage("/Index");
            }

            UserFullName = $"{user.FirstName} {user.LastName}".Trim();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            _logger.LogInformation("OnPostAsync called.");

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("ModelState invalid.");
                UserFullName = $"{user.FirstName} {user.LastName}".Trim();
                return Page();
            }

            // Creăm manual OwnerRequest fără să ne bazăm pe bind la proprietăți sensibile
            var ownerRequest = new OwnerRequest
            {
                CompanyName = Input.CompanyName,
                TaxIdentificationNumber = Input.TaxIdentificationNumber,
                ApplicationUserId = user.Id,           // Setăm noi ID-ul
                Status = "Pending",
                RequestDate = DateTime.Now
            };

            try
            {
                _context.OwnerRequests.Add(ownerRequest);
                await _context.SaveChangesAsync();
                _logger.LogInformation("OwnerRequest salvat cu succes.");

                // Opțional: mesaj de succes
                TempData["SuccessMessage"] = "Cererea ta a fost trimisă și este în așteptarea aprobării.";
                return RedirectToPage("/Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Eroare la salvarea OwnerRequest.");
                ModelState.AddModelError(string.Empty, "A apărut o eroare. Încearcă din nou.");
                UserFullName = $"{user.FirstName} {user.LastName}".Trim();
                return Page();
            }
        }
    }
}