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
        public ApplicationUser CurrentUser { get; set; }
        public int CurrentUserRank { get; set; }

        public async Task OnGetAsync()
        {
            // 1. Obținem userul curent
            CurrentUser = await _userManager.GetUserAsync(User);

            // 2. Luăm top 20 utilizatori direct din DB (ordonat după XP)
            // Excludem Adminii dacă au un XP uriaș care ar strica clasamentul
            TopUsers = await _context.Users
                .OrderByDescending(u => u.XP)
                .Take(20)
                .ToListAsync();

            // 3. Calculăm Rank-ul utilizatorului curent
            // Rank = (Numărul de oameni care au XP mai mare decât mine) + 1
            if (CurrentUser != null)
            {
                CurrentUserRank = await _context.Users
                    .CountAsync(u => u.XP > CurrentUser.XP) + 1;
            }
        }
    }
}