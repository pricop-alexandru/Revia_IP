using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Revia.Data;
using Revia.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Revia.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Location> TopLocations { get; set; } = new();

        public async Task OnGetAsync()
        {
            // Aducem toate localurile aprobate cu review-uri
            // (Facem sortarea în memorie pentru că AverageRating nu e în SQL)
            var allLocations = await _context.Locations
                .Include(l => l.Reviews)
                .Where(l => l.Status == "Approved")
                .ToListAsync();

            TopLocations = allLocations
                .OrderByDescending(l => l.AverageRating)
                .ThenByDescending(l => l.ReviewCount)
                .Take(3)
                .ToList();
        }
    }
}