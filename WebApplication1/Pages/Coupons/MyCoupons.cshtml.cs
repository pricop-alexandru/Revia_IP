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
    public class MyCouponsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public MyCouponsModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public List<UserCoupon> ActiveCoupons { get; set; }
        public List<UserCoupon> ClaimedCoupons { get; set; }

        public async Task OnGetAsync()
        {
            var userId = _userManager.GetUserId(User);

            var allCoupons = await _context.UserCoupons
                .Include(uc => uc.Coupon)
                .ThenInclude(c => c.Location)
                .Where(uc => uc.UserId == userId)
                .OrderBy(uc => uc.Coupon.ExpirationDate)
                .ToListAsync();

            ActiveCoupons = allCoupons
                .Where(uc => !uc.IsClaimed && uc.Coupon.ExpirationDate > DateTime.Now)
                .ToList();

            ClaimedCoupons = allCoupons
                .Where(uc => uc.IsClaimed)
                .OrderByDescending(uc => uc.DateClaimed)
                .ToList();
        }

        public async Task<IActionResult> OnPostClaimAsync(int id)
        {
            var uc = await _context.UserCoupons.FindAsync(id);
            if (uc == null || uc.UserId != _userManager.GetUserId(User)) return NotFound();

            uc.IsClaimed = true;
            uc.DateClaimed = DateTime.Now;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Cupon revendicat cu succes!";
            return RedirectToPage();
        }
    }
}