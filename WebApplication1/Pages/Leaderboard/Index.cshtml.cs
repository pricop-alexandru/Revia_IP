using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Revia.Data;
using Revia.Models;

namespace Revia.Pages.Leaderboard
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public IndexModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public List<ApplicationUser> TopUsers { get; set; } = new();

        public async Task OnGetAsync()
        {
            // Luăm toți userii
            var allUsers = await _context.Users.ToListAsync();

            // Filtrăm în memorie pentru a exclude Adminii (e mai sigur cu UserManager)
            var nonAdminUsers = new List<ApplicationUser>();

            foreach (var user in allUsers)
            {
                if (!await _userManager.IsInRoleAsync(user, UserRoles.Admin))
                {
                    nonAdminUsers.Add(user);
                }
            }

            TopUsers = nonAdminUsers
                .OrderByDescending(u => u.XP)
                .Take(20)
                .ToList();
        }
    }
}