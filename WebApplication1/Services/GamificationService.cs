using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Revia.Data;
using Revia.Models;

namespace Revia.Services
{
    public class GamificationService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public record LevelProgress
        {
            public int CurrentXP { get; init; }
            public int LevelStartXP { get; init; } // XP total necesar pentru a atinge nivelul curent
            public int NextLevelXP { get; init; } // XP total necesar pentru a atinge nivelul următor
            public int XPToNextLevel { get; init; } // Diferența: NextLevelXP - LevelStartXP
            public int XPProgressInLevel { get; init; } // Diferența: CurrentXP - LevelStartXP
            public double Percentage { get; init; }
        }

        public LevelProgress GetLevelProgress(ApplicationUser user)
        {
            int currentXP = user.XP;
            int level = user.Level;

            int requiredXPForNextLevel = 500;
            int totalXPForCurrentLevel = 0;

            // Calculăm XP-ul TOTAL CUMULATIV necesar pentru nivelul CURENT și cel următor
            // XP necesar pentru Lvl 1: 0
            // XP necesar pentru Lvl 2: 500
            // XP necesar pentru Lvl 3: 1000
            // etc.
            for (int i = 1; i < level; i++) // Iterează până la Nivelul Curemnt - 1
            {
                totalXPForCurrentLevel += requiredXPForNextLevel;
                requiredXPForNextLevel *= 2;
            }

            // XP total necesar pentru a ajunge la nivelul următor
            int totalXPForNextLevel = totalXPForCurrentLevel + requiredXPForNextLevel;

            // XP Progression în Levelul curent
            int xpProgressInLevel = currentXP - totalXPForCurrentLevel;

            // XP total de câștigat în acest nivel (de la începutul Lvl curent până la începutul Lvl următor)
            int xpToNextLevel = requiredXPForNextLevel;

            double percentage = xpToNextLevel > 0
                ? (double)xpProgressInLevel / xpToNextLevel * 100
                : 100; // Dacă nivelul este maxim, arată 100%

            return new LevelProgress
            {
                CurrentXP = currentXP,
                LevelStartXP = totalXPForCurrentLevel,
                NextLevelXP = totalXPForNextLevel,
                XPToNextLevel = xpToNextLevel,
                XPProgressInLevel = xpProgressInLevel,
                Percentage = percentage
            };
        }

        public GamificationService(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public async Task AwardXPAsync(ApplicationUser user, int xpAmount, string reason = "")
        {
            user.XP += xpAmount;
            user.Level = CalculateLevel(user.XP);

            // Forțăm update complet
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            await _userManager.UpdateAsync(user);
            await PromoteToLocalGuideIfEligible(user, reason);
        }

        private int CalculateLevel(int xp)
        {
            int level = 1;
            int required = 500;

            while (xp >= required)
            { 
                level++;
                required *= 2;
            }
            return level;
        }

        private async Task PromoteToLocalGuideIfEligible(ApplicationUser user, string reason)
        {
            if (user.Level >= 5 && !await _userManager.IsInRoleAsync(user, UserRoles.LocalGuide))
            {
                await _userManager.AddToRoleAsync(user, UserRoles.LocalGuide);

                var exists = await _context.LocalGuides
                    .AnyAsync(lg => lg.ApplicationUserId == user.Id);

                if (!exists)
                {
                    _context.LocalGuides.Add(new LocalGuide
                    {
                        ApplicationUserId = user.Id
                    });
                    await _context.SaveChangesAsync();
                }

                // Poți loga sau trimite notificare aici
            }
        }
        public async Task CheckAndAwardCouponsAsync(string userId, int locationId, bool justApproved = false)
        {
            // Obținem utilizatorul pentru a-i verifica nivelul actual
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return;

            // 1. Verificăm dacă userul a scris MĂCAR O recenzie aprobată aici
            bool hasUserContributed = await _context.Reviews
                .AnyAsync(r => r.UserId == userId && r.LocationId == locationId && r.Status == "Approved");

            if (!hasUserContributed && justApproved) hasUserContributed = true;
            if (!hasUserContributed) return;

            // 2. Calculăm numărul TOTAL de recenzii aprobate ale localului
            int locationTotalReviews = await _context.Reviews
                .CountAsync(r => r.LocationId == locationId && r.Status == "Approved");

            // 3. Căutăm cupoane active unde pragul de popularitate ȘI pragul de nivel sunt atinse
            var availableCoupons = await _context.Coupons
                .Where(c => c.LocationId == locationId
                            && c.IsActive
                            && c.ExpirationDate > DateTime.Now
                            && c.RequiredLevel <= user.Level) // LOGICA DE NIVEL AICI
                .ToListAsync();

            foreach (var coupon in availableCoupons)
            {
                if (coupon.RequiredReviewsCount <= locationTotalReviews)
                {
                    bool alreadyHas = await _context.UserCoupons
                        .AnyAsync(uc => uc.UserId == userId && uc.CouponId == coupon.Id);

                    if (!alreadyHas)
                    {
                        _context.UserCoupons.Add(new UserCoupon
                        {
                            UserId = userId,
                            CouponId = coupon.Id,
                            DateReceived = DateTime.Now,
                            IsClaimed = false
                        });

                        _context.Notifications.Add(new Notification
                        {
                            UserId = userId,
                            Text = $"Bravo! Ai deblocat un cupon exclusiv la '{coupon.Location?.Name ?? "Local"}'! ({coupon.Title})",
                            Date = DateTime.Now
                        });
                    }
                }
            }
            await _context.SaveChangesAsync();
        }
    }
}