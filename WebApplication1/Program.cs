using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Revia.Data;
using Revia.Models;
using Revia.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Conexiunea la SQL Server
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// 2. Configurarea Identity (AICI ESTE CHEIA PENTRU ROLURI)
// .AddRoles<IdentityRole>() este obligatoriu!
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false; // Pentru dev, punem false
    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = true;
})
    .AddRoles<IdentityRole>() // <--- IMPORTANT: Activează sistemul de roluri
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddRazorPages();
builder.Services.AddScoped<Revia.Components.PendingCountsViewComponent>();
builder.Services.AddScoped<GamificationService>();

var app = builder.Build();

// ... (partea de Error Handling, HttpsRedirection etc.) ...
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// 3. Activăm Autentificarea și Autorizarea
app.UseAuthentication();
app.UseAuthorization();

// 4. Apelăm SEEDING-ul (DbInitializer)
// Asta se execută de fiecare dată când pornește aplicația și verifică dacă există adminul
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        // Apelăm metoda statică pe care tocmai am scris-o
        await DbInitializer.Initialize(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred seeding the DB.");
    }
}

app.MapRazorPages();

app.Run();