using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SegundoEjercicio.Data;        
using SegundoEjercicio.Models;      

namespace SegundoEjercicio.Pages.Admin.Usuarios;

[Authorize(Roles = "Bibliotecario")]
public class IndexModel : PageModel
{
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly LibraryContext _db;

    public IndexModel(UserManager<AppUser> userManager,
                      RoleManager<IdentityRole> roleManager,
                      LibraryContext db)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _db = db;
    }

    public record Item(string Id, string? FullName, string? Email, IList<string> Roles, bool IsLocked);
    public List<Item> Items { get; set; } = new();

    public async Task OnGetAsync()
    {
        var users = await _userManager.Users.OrderBy(u => u.Email).ToListAsync();

        foreach (var u in users)
        {
            // nombre con fallback a Member o al prefijo del email
            var memberName = await _db.Members
                .Where(m => m.AppUserId == u.Id)
                .Select(m => m.FullName)
                .FirstOrDefaultAsync();

            var displayName = string.IsNullOrWhiteSpace(u.FullName)
                ? (string.IsNullOrWhiteSpace(memberName)
                    ? (u.Email?.Split('@').FirstOrDefault() ?? "(sin nombre)")
                    : memberName)
                : u.FullName;

            var roles = await _userManager.GetRolesAsync(u);
            var isLocked = await _userManager.IsLockedOutAsync(u);

            Items.Add(new Item(u.Id, displayName, u.Email, roles, isLocked));
        }
    }

    // ===== Roles  =====
    public async Task<IActionResult> OnPostMakeBiblioAsync(string id)
    {
        var u = await _userManager.FindByIdAsync(id);
        if (u is null) return RedirectToPage();

        if (!await _roleManager.RoleExistsAsync("Bibliotecario"))
            await _roleManager.CreateAsync(new IdentityRole("Bibliotecario"));

        await _userManager.RemoveFromRoleAsync(u, "Socio");
        await _userManager.AddToRoleAsync(u, "Bibliotecario");

        TempData["msg"] = $"Usuario {u.Email} ahora es Bibliotecario.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostMakeSocioAsync(string id)
    {
        var u = await _userManager.FindByIdAsync(id);
        if (u is null) return RedirectToPage();

        if (!await _roleManager.RoleExistsAsync("Socio"))
            await _roleManager.CreateAsync(new IdentityRole("Socio"));

        await _userManager.RemoveFromRoleAsync(u, "Bibliotecario");
        await _userManager.AddToRoleAsync(u, "Socio");

        TempData["msg"] = $"Usuario {u.Email} ahora es Socio.";
        return RedirectToPage();
    }

    // ===== Banear (lockout indefinido) =====
    public async Task<IActionResult> OnPostBanAsync(string id)
    {
        var me = await _userManager.GetUserAsync(User);
        if (me!.Id == id) { TempData["msg"] = "No podés banearte a vos mismo."; return RedirectToPage(); }

        var u = await _userManager.FindByIdAsync(id);
        if (u is null) { TempData["msg"] = "Usuario no encontrado."; return RedirectToPage(); }

        await _userManager.SetLockoutEnabledAsync(u, true);
        await _userManager.SetLockoutEndDateAsync(u, DateTimeOffset.MaxValue);
        TempData["msg"] = $"Usuario {u.Email} baneado.";
        return RedirectToPage();
    }

    // ===== Desbanear =====
    public async Task<IActionResult> OnPostUnbanAsync(string id)
    {
        var u = await _userManager.FindByIdAsync(id);
        if (u is null) { TempData["msg"] = "Usuario no encontrado."; return RedirectToPage(); }

        await _userManager.SetLockoutEndDateAsync(u, null);
        await _userManager.ResetAccessFailedCountAsync(u);
        TempData["msg"] = $"Usuario {u.Email} reactivado.";
        return RedirectToPage();
    }

    // ===== Eliminar (solo bloquea si hay préstamo ACTIVO) =====
    public async Task<IActionResult> OnPostDeleteAsync(string id)
    {
        var me = await _userManager.GetUserAsync(User);
        if (me!.Id == id) { TempData["msg"] = "No podés eliminarte a vos mismo."; return RedirectToPage(); }

        var u = await _userManager.FindByIdAsync(id);
        if (u is null) { TempData["msg"] = "Usuario no encontrado."; return RedirectToPage(); }

        // No permitir borrar al ÚLTIMO bibliotecario
        if (await _userManager.IsInRoleAsync(u, "Bibliotecario"))
        {
            var biblioCount = (await _userManager.GetUsersInRoleAsync("Bibliotecario")).Count;
            if (biblioCount <= 1)
            {
                TempData["msg"] = "No se puede eliminar al único Bibliotecario.";
                return RedirectToPage();
            }
        }

        // Buscar MemberId (si lo tiene)
        var memberId = await _db.Members
            .Where(m => m.AppUserId == u.Id)
            .Select(m => (int?)m.Id)
            .FirstOrDefaultAsync();

        if (memberId != null)
        {
            // Bloquea SOLO si hay préstamo ACTIVO
            var tienePrestamoActivo = await _db.Loans
                .AnyAsync(l => l.MemberId == memberId && l.ReturnDate == null);

            if (tienePrestamoActivo)
            {
                TempData["msg"] = "No se puede eliminar: el usuario tiene un préstamo ACTIVO.";
                return RedirectToPage();
            }

            // Limpieza total de historial y solicitudes (cualquier estado)
            await using var tx = await _db.Database.BeginTransactionAsync();

            var loansHist = _db.Loans.Where(l => l.MemberId == memberId);
            var reqsTodos = _db.LoanRequests.Where(r => r.MemberId == memberId);

            _db.Loans.RemoveRange(loansHist);
            _db.LoanRequests.RemoveRange(reqsTodos);

            // Borrar Member
            var member = new Member { Id = memberId.Value };
            _db.Attach(member);
            _db.Remove(member);

            await _db.SaveChangesAsync();
            await tx.CommitAsync();
        }

        // Finalmente, borro la  cuenta de Identity
        var result = await _userManager.DeleteAsync(u);
        TempData["msg"] = result.Succeeded
            ? $"Usuario {u.Email} eliminado."
            : string.Join(" | ", result.Errors.Select(e => e.Description));

        return RedirectToPage();
    }
}
