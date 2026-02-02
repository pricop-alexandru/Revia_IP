using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Revia.Data;
using Revia.Models;

namespace Revia.Pages.Coupons
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

        public Location Location { get; set; }
        public List<CouponViewModel> CouponsList { get; set; } = new();
        public bool IsOwnerOfLocation { get; set; }

        // Proprietate necesară pentru a verifica nivelul în UI (Index.cshtml)
        public int UserLevel { get; set; }

        public class CouponViewModel
        {
            public Coupon Coupon { get; set; }
            public bool UserHasIt { get; set; } // Dacă userul are deja cuponul în portofel
            public bool IsClaimed { get; set; } // Dacă a fost deja scanat/folosit
            public int LocationTotalReviews { get; set; } // Totalul de review-uri aprobate ale locației
        }

        public async Task<IActionResult> OnGetAsync(int locationId)
        {
            // 1. Încărcăm locația și proprietarul pentru a verifica drepturile de editare
            Location = await _context.Locations
                .Include(l => l.Owner)
                .FirstOrDefaultAsync(l => l.Id == locationId);

            if (Location == null) return NotFound();

            // 2. Obținem datele utilizatorului logat
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            // 3. Setăm starea paginii
            IsOwnerOfLocation = Location.Owner?.ApplicationUserId == currentUser.Id;
            UserLevel = currentUser.Level;

            // 4. Preluăm toate cupoanele active ale locației
            // Sortăm mai întâi după nivelul cerut (RequiredLevel) pentru a arăta progresia
            var locationCoupons = await _context.Coupons
                .Where(c => c.LocationId == locationId && c.IsActive && c.ExpirationDate > DateTime.Now)
                .OrderBy(c => c.RequiredLevel)
                .ThenBy(c => c.RequiredReviewsCount)
                .ToListAsync();

            // 5. Vedem ce cupoane deține deja userul la această locație specifică
            var myUserCoupons = await _context.UserCoupons
                .Where(uc => uc.UserId == currentUser.Id && uc.Coupon.LocationId == locationId)
                .ToListAsync();

            // 6. Calculăm popularitatea totală a locației (doar review-uri aprobate)
            int locationTotalReviews = await _context.Reviews
                .CountAsync(r => r.LocationId == locationId && r.Status == "Approved");

            // 7. Construim lista pentru View
            CouponsList = new List<CouponViewModel>();

            foreach (var coupon in locationCoupons)
            {
                var userCoupon = myUserCoupons.FirstOrDefault(uc => uc.CouponId == coupon.Id);

                CouponsList.Add(new CouponViewModel
                {
                    Coupon = coupon,
                    UserHasIt = userCoupon != null,
                    IsClaimed = userCoupon?.IsClaimed ?? false,
                    LocationTotalReviews = locationTotalReviews
                });
            }

            return Page();
        }

        // Handler pentru ștergerea cuponului (disponibil doar ownerului)
        public async Task<IActionResult> OnPostDeleteAsync(int couponId, int locationId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var coupon = await _context.Coupons
                .Include(c => c.Location)
                .ThenInclude(l => l.Owner)
                .FirstOrDefaultAsync(c => c.Id == couponId);

            // Verificăm dacă cel care șterge este chiar proprietarul locației
            if (coupon != null && coupon.Location.Owner.ApplicationUserId == user.Id)
            {
                _context.Coupons.Remove(coupon);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Oferta a fost eliminată cu succes.";
            }
            else
            {
                TempData["ErrorMessage"] = "Nu ai permisiunea de a șterge această ofertă.";
            }

            return RedirectToPage(new { locationId = locationId });
        }
    }
}