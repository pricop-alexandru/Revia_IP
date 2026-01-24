using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Revia.Data;
using Revia.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Revia.Pages.Admin
{
    [Authorize(Roles = UserRoles.Admin)]
    public class OwnerRequestsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<OwnerRequestsModel> _logger;

        public OwnerRequestsModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager, ILogger<OwnerRequestsModel> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public IList<OwnerRequest> PendingRequests { get; set; }

        public async Task OnGetAsync()
        {
            PendingRequests = await _context.OwnerRequests
                .Include(or => or.User)
                .Where(or => or.Status == "Pending")
                .OrderByDescending(or => or.RequestDate)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostApproveAsync(int id)
        {
            var request = await _context.OwnerRequests
                .Include(or => or.User)
                .FirstOrDefaultAsync(or => or.Id == id);

            if (request == null) return NotFound();

            var user = request.User;
            if (user == null) return NotFound();

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                request.Status = "Approved";

                var owner = new Owner
                {
                    CompanyName = request.CompanyName,
                    TaxIdentificationNumber = request.TaxIdentificationNumber,
                    IsVerified = true,
                    ApplicationUserId = user.Id
                };
                _context.Owners.Add(owner);

                if (!await _userManager.IsInRoleAsync(user, UserRoles.Owner))
                    await _userManager.AddToRoleAsync(user, UserRoles.Owner);

                if (await _userManager.IsInRoleAsync(user, UserRoles.Client))
                    await _userManager.RemoveFromRoleAsync(user, UserRoles.Client);

                var notification = new Notification
                {
                    UserId = user.Id,
                    Text = $"Felicitări! Cererea ta pentru compania '{request.CompanyName}' a fost aprobată. Acum ești Owner.",
                    Date = DateTime.Now,
                    IsRead = false
                };
                _context.Notifications.Add(notification);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["SuccessMessage"] = $"Cererea utilizatorului {user.UserName} a fost aprobată.";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, $"Eroare la aprobarea cererii {id}");
                TempData["ErrorMessage"] = "Eroare la aprobare.";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRejectAsync(int id)
        {
            // Trebuie să includem User-ul ca să îi putem lua ID-ul pentru notificare
            var request = await _context.OwnerRequests
                .Include(or => or.User)
                .FirstOrDefaultAsync(or => or.Id == id);

            if (request == null) return NotFound();

            request.Status = "Rejected";

            // --- COD NOU: NOTIFICARE ---
            var notification = new Notification
            {
                UserId = request.User.Id, // Folosim ID-ul userului din cerere
                Text = $"Ne pare rău, cererea ta pentru compania '{request.CompanyName}' a fost respinsă. Contactează un admin pentru detalii.",
                Date = DateTime.Now,
                IsRead = false
            };
            _context.Notifications.Add(notification);
            // ---------------------------

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Cererea a fost respinsă și utilizatorul a fost notificat.";
            return RedirectToPage();
        }
    }
}