using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Revia.Data;
using Revia.Models;

namespace Revia.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Location> OurPicks { get; set; } = new();
        public List<Location> TrendingSpots { get; set; } = new();

        public async Task OnGetAsync()
        {
            // 1. Our Picks: Locații Parteneri Oficiali (IsOfficialPartner == true)
            OurPicks = await _context.Locations
                .Include(l => l.Reviews)
                .Where(l => l.Status == "Approved" && l.IsOfficialPartner)
                .Take(3)
                .ToListAsync();

            // 2. Trending Spots: Cele mai multe recenzii aprobate
            // Sortăm în memorie pentru a folosi proprietatea ReviewCount calculată
            var approvedLocations = await _context.Locations
                .Include(l => l.Reviews)
                .Where(l => l.Status == "Approved")
                .ToListAsync();

            TrendingSpots = approvedLocations
                .OrderByDescending(l => l.Reviews.Count(r => r.Status == "Approved"))
                .ThenByDescending(l => l.AverageRating)
                .Take(6)
                .ToList();
        }
    }
}