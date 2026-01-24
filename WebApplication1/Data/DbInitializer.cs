using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Revia.Models;

namespace Revia.Data
{
    public static class DbInitializer
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            context.Database.EnsureCreated();

            string[] roleNames = { UserRoles.Admin, UserRoles.Owner, UserRoles.LocalGuide, UserRoles.Client };
            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // ADMIN
            var adminEmail = "admin@revia.ro";
            if (await userManager.FindByEmailAsync(adminEmail) == null)
            {
                var adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FirstName = "Revia",
                    LastName = "Administrator",
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(adminUser, "Admin123!");
                await userManager.AddToRoleAsync(adminUser, UserRoles.Admin);
            }

            // OWNER
            var ownerEmail = "owner@revia.ro";
            if (await userManager.FindByEmailAsync(ownerEmail) == null)
            {
                var ownerUser = new ApplicationUser
                {
                    UserName = ownerEmail,
                    Email = ownerEmail,
                    FirstName = "Ion",
                    LastName = "Patron",
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(ownerUser, "Owner123!");
                await userManager.AddToRoleAsync(ownerUser, UserRoles.Owner);

                var ownerEntity = new Owner
                {
                    ApplicationUserId = ownerUser.Id,
                    CompanyName = "Revia Pizza SRL",
                    TaxIdentificationNumber = "RO12345678",
                    IsVerified = true
                };
                context.Owners.Add(ownerEntity);
                await context.SaveChangesAsync();
            }

            // LOCAL GUIDE
            var guideEmail = "guide@revia.ro";
            if (await userManager.FindByEmailAsync(guideEmail) == null)
            {
                var guideUser = new ApplicationUser
                {
                    UserName = guideEmail,
                    Email = guideEmail,
                    FirstName = "Maria",
                    LastName = "Calator",
                    EmailConfirmed = true,
                    XP = 4050,        // XP inițial
                    Level = 4        // Level inițial (sau calculează cu metoda ta de dublare)
                };

                var result = await userManager.CreateAsync(guideUser, "Guide123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(guideUser, UserRoles.LocalGuide);

                    // Creăm doar legătura minimă în tabela LocalGuides
                    var guideEntity = new LocalGuide
                    {
                        ApplicationUserId = guideUser.Id
                    };

                    context.LocalGuides.Add(guideEntity);
                    await context.SaveChangesAsync();
                }
            }

            // CLIENT
            var clientEmail = "client@revia.ro";
            if (await userManager.FindByEmailAsync(clientEmail) == null)
            {
                var clientUser = new ApplicationUser
                {
                    UserName = clientEmail,
                    Email = clientEmail,
                    FirstName = "Alex",
                    LastName = "Vizitator",
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(clientUser, "Client123!");
                await userManager.AddToRoleAsync(clientUser, UserRoles.Client);
            }

            // TEST LOCATION
            var dbOwnerUser = await userManager.FindByEmailAsync("owner@revia.ro");
            if (dbOwnerUser != null)
            {
                var ownerProfile = await context.Owners.FirstOrDefaultAsync(o => o.ApplicationUserId == dbOwnerUser.Id);
                if (ownerProfile != null)
                {
                    string testLocationName = "Revia Bistro & Tech";
                    if (!await context.Locations.AnyAsync(l => l.Name == testLocationName))
                    {
                        var testLocation = new Location
                        {
                            Name = testLocationName,
                            Description = "Cel mai bun loc pentru a testa funcționalități .NET și burgeri delicioși.",
                            Address = "Bulevardul Eroilor nr. 5, Brașov",
                            ImageUrl = "https://images.unsplash.com/photo-1552566626-52f8b828add9?q=80&w=1000&auto=format&fit=crop",
                            OwnerId = ownerProfile.Id,
                            Status = "Approved" // Deja aprobat pentru test
                        };
                        context.Locations.Add(testLocation);
                        await context.SaveChangesAsync();
                    }
                }
            }
        }
    }
}