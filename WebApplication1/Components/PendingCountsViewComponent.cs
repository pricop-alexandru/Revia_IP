using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Revia.Data;
using System.Threading.Tasks;

namespace Revia.Components
{
    public class PendingCountsViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;

        public PendingCountsViewComponent(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<IViewComponentResult> InvokeAsync()
        {
            var model = await GetPendingCountsModelAsync();

            return View(model);
        }
        public async Task<PendingCountsViewModel> GetPendingCountsModelAsync()
        {
            var pendingLocations = await _context.Locations
                .CountAsync(l => l.Status == "Pending");

            var pendingOwnerRequests = await _context.OwnerRequests
                .CountAsync(or => or.Status == "Pending");

            return new PendingCountsViewModel
            {
                PendingLocations = pendingLocations,
                PendingOwnerRequests = pendingOwnerRequests
            };
        }
    }

    public class PendingCountsViewModel
    {
        public int PendingLocations { get; set; }
        public int PendingOwnerRequests { get; set; }
    }
}