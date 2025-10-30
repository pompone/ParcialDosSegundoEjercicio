using System.Linq;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SegundoEjercicio.Data;
using SegundoEjercicio.Models;

var builder = WebApplication.CreateBuilder(args);

// --- Npgsql: comportamiento legacy para timestamps (evita sorpresas) ---
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

// --- ConnectionString: appsettings / user-secrets / env var (Render) ---
var cs = builder.Configuration.GetConnectionString("DefaultConnection")
         ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");

if (string.IsNullOrWhiteSpace(cs))
    throw new InvalidOperationException("No se encontró la cadena de conexión DefaultConnection.");

// --- DbContexts ---
builder.Services.AddDbContext<LibraryContext>(opt =>
    opt.UseNpgsql(cs, npg => npg.EnableRetryOnFailure()));

builder.Services.AddDbContext<AuthDbContext>(opt =>
    opt.UseNpgsql(cs, npg => npg.EnableRetryOnFailure()));

// --- Identity ---
builder.Services
    .AddDefaultIdentity<AppUser>(opts =>
    {
        opts.SignIn.RequireConfirmedAccount = false;
        opts.Password.RequireDigit = true;
        opts.Password.RequireLowercase = true;
        opts.Password.RequireUppercase = true;
        opts.Password.RequireNonAlphanumeric = false;
        opts.Password.RequiredLength = 8;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<AuthDbContext>()
    .AddDefaultUI()
    .AddDefaultTokenProviders();

// --- Cookies ---
builder.Services.ConfigureApplicationCookie(o =>
{
    o.LoginPath = "/cuenta/ingresar";
    o.AccessDeniedPath = "/Identity/Account/AccessDenied";
    o.Cookie.HttpOnly = true;
    o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    o.Cookie.SameSite = SameSiteMode.Lax;
    o.SlidingExpiration = true;
});

// --- Razor + Autorización ---
builder.Services.AddRazorPages(o =>
{
    o.Conventions.AuthorizeFolder("/");

    // Permitir anónimo en páginas de Identity necesarias
    o.Conventions.AllowAnonymousToAreaPage("Identity", "/Account/Login");
    o.Conventions.AllowAnonymousToAreaPage("Identity", "/Account/Register");
    o.Conventions.AllowAnonymousToAreaPage("Identity", "/Account/AccessDenied");
    o.Conventions.AllowAnonymousToPage("/Error");

    // Alias de rutas en español
    o.Conventions.AddAreaPageRoute("Identity", "/Account/Login", "cuenta/ingresar");
    o.Conventions.AddAreaPageRoute("Identity", "/Account/Register", "cuenta/registrarme");
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SoloBiblio", p => p.RequireRole("Bibliotecario"));
});

var app = builder.Build();

// --- Reverse proxy headers (Render) ---
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

// --- Manejo de errores según entorno ---
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// --- Migraciones y seed seguros ---
try
{
    using var scope = app.Services.CreateScope();
    var sp = scope.ServiceProvider;

    var lib = sp.GetRequiredService<LibraryContext>();
    await lib.Database.MigrateAsync();
    await DbInitializer.SeedAsync(lib);

    var auth = sp.GetRequiredService<AuthDbContext>();
    await auth.Database.MigrateAsync();

    await SeedRolesAndUsersAsync(sp);
    await SeedHelpers.BackfillMembersForSociosAsync(sp);

    app.Logger.LogInformation("Migraciones y seed OK");
}
catch (Exception ex)
{
    app.Logger.LogError(ex, "Fallo en migraciones/seed (la app sigue levantando para ver logs).");
}

// Home → /Libros
app.MapGet("/", ctx =>
{
    ctx.Response.Redirect("/Libros");
    return Task.CompletedTask;
});

app.MapRazorPages();
app.MapControllers();

app.Run();


// ====== SEED: roles + usuarios demo (con reset de contraseña si existe) ======
static async Task SeedRolesAndUsersAsync(IServiceProvider services)
{
    var roleMgr = services.GetRequiredService<RoleManager<IdentityRole>>();
    var userMgr = services.GetRequiredService<UserManager<AppUser>>();

    // Roles requeridos
    foreach (var r in new[] { "Bibliotecario", "Socio" })
        if (!await roleMgr.RoleExistsAsync(r))
            await roleMgr.CreateAsync(new IdentityRole(r));

    // Admin
    const string adminEmail = "biblio@demo.com";
    const string adminPass  = "Biblio123!";

    var admin = await userMgr.FindByEmailAsync(adminEmail);
    if (admin == null)
    {
        admin = new AppUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true,
            FullName = "Admin Biblioteca"
        };
        var create = await userMgr.CreateAsync(admin, adminPass);
        if (!create.Succeeded)
            throw new Exception("No se pudo crear el admin: " + string.Join("; ", create.Errors.Select(e => e.Description)));
    }
    else
    {
        var token = await userMgr.GeneratePasswordResetTokenAsync(admin);
        await userMgr.ResetPasswordAsync(admin, token, adminPass);
        await userMgr.SetLockoutEndDateAsync(admin, null);
        await userMgr.ResetAccessFailedCountAsync(admin);
        admin.EmailConfirmed = true;
        await userMgr.UpdateAsync(admin);
    }
    if (!await userMgr.IsInRoleAsync(admin, "Bibliotecario"))
        await userMgr.AddToRoleAsync(admin, "Bibliotecario");

    // Socio demo (opcional)
    const string socioEmail = "socio@demo.com";
    const string socioPass  = "Socio123!";
    var socio = await userMgr.FindByEmailAsync(socioEmail);
    if (socio == null)
    {
        socio = new AppUser
        {
            UserName = socioEmail,
            Email = socioEmail,
            EmailConfirmed = true,
            FullName = "Socio Demo"
        };
        var createS = await userMgr.CreateAsync(socio, socioPass);
        if (!createS.Succeeded)
            throw new Exception("No se pudo crear el socio: " + string.Join("; ", createS.Errors.Select(e => e.Description)));
    }
    else
    {
        var tokenS = await userMgr.GeneratePasswordResetTokenAsync(socio);
        await userMgr.ResetPasswordAsync(socio, tokenS, socioPass);
        await userMgr.SetLockoutEndDateAsync(socio, null);
        await userMgr.ResetAccessFailedCountAsync(socio);
        socio.EmailConfirmed = true;
        await userMgr.UpdateAsync(socio);
    }
    if (!await userMgr.IsInRoleAsync(socio, "Socio"))
        await userMgr.AddToRoleAsync(socio, "Socio");
}


