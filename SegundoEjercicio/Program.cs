using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SegundoEjercicio.Data;
using SegundoEjercicio.Models;

var builder = WebApplication.CreateBuilder(args);

// --- Npgsql switch común (evita sorpresas con timestamps) ---
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

// --- ConnectionString: appsettings / secrets / env var (Render) ---
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
    o.Conventions.AllowAnonymousToAreaPage("Identity", "/Account/Login");
    o.Conventions.AllowAnonymousToAreaPage("Identity", "/Account/Register");
    o.Conventions.AllowAnonymousToPage("/Error");

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

if (!app.Environment.IsDevelopment())
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

// ====== Seed Roles y Usuarios demo ======
static async Task SeedRolesAndUsersAsync(IServiceProvider services)
{
    var roleMgr = services.GetRequiredService<RoleManager<IdentityRole>>();
    var userMgr = services.GetRequiredService<UserManager<AppUser>>();

    string[] roles = { "Bibliotecario", "Socio" };
    foreach (var r in roles)
        if (!await roleMgr.RoleExistsAsync(r))
            await roleMgr.CreateAsync(new IdentityRole(r));

    var bEmail = "biblio@demo.com";
    var b = await userMgr.FindByEmailAsync(bEmail) ?? new AppUser
    {
        UserName = bEmail,
        Email = bEmail,
        EmailConfirmed = true,
        FullName = "Admin Biblioteca"
    };
    if (b.Id == null) await userMgr.CreateAsync(b, "Biblio123!");
    if (!await userMgr.IsInRoleAsync(b, "Bibliotecario")) await userMgr.AddToRoleAsync(b, "Bibliotecario");

    var sEmail = "socio@demo.com";
    var s = await userMgr.FindByEmailAsync(sEmail) ?? new AppUser
    {
        UserName = sEmail,
        Email = sEmail,
        EmailConfirmed = true,
        FullName = "Socio Demo"
    };
    if (s.Id == null) await userMgr.CreateAsync(s, "Socio123!");
    if (!await userMgr.IsInRoleAsync(s, "Socio")) await userMgr.AddToRoleAsync(s, "Socio");
}

