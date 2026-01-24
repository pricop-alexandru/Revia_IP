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

        public class CouponViewModel
        {
            public Coupon Coupon { get; set; }
            public bool UserHasIt { get; set; } // Îl are deja?
            public bool IsClaimed { get; set; } // L-a consumat?
            public int LocationTotalReviews { get; set; } // Total review-uri locație
        }

        public async Task<IActionResult> OnGetAsync(int locationId)
        {
            Location = await _context.Locations
                .Include(l => l.Owner)
                .FirstOrDefaultAsync(l => l.Id == locationId);

            if (Location == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            IsOwnerOfLocation = Location.Owner.ApplicationUserId == currentUser.Id;

            // Toate cupoanele active ale locației
            var locationCoupons = await _context.Coupons
                .Where(c => c.LocationId == locationId && c.IsActive && c.ExpirationDate > DateTime.Now)
                .OrderBy(c => c.RequiredReviewsCount)
                .ToListAsync();

            // Ce cupoane deține deja userul?
            var myUserCoupons = await _context.UserCoupons
                .Where(uc => uc.UserId == currentUser.Id && uc.Coupon.LocationId == locationId)
                .ToListAsync();
            // Total review-uri locație (pentru afișare în view)
            int locationTotalReviews = await _context.Reviews
                .CountAsync(r => r.LocationId == locationId && r.Status == "Approved");
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

        // Ownerul poate șterge un cupon
        public async Task<IActionResult> OnPostDeleteAsync(int couponId, int locationId)
        {
            var coupon = await _context.Coupons
                .Include(c => c.Location).ThenInclude(l => l.Owner)
                .FirstOrDefaultAsync(c => c.Id == couponId);

            var user = await _userManager.GetUserAsync(User);

            if (coupon != null && coupon.Location.Owner.ApplicationUserId == user.Id)
            {
                _context.Coupons.Remove(coupon);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Cupon șters.";
            }
            return RedirectToPage(new { locationId = locationId });
        }
    }
}