using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Revia.Data;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Revia.Components
{
    public class NotificationsViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;

        public NotificationsViewComponent(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var userId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Content("");

            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .OrderByDescending(n => n.Date)
                .Take(5)
                .ToListAsync();

            return View(notifications);
        }
    }
}