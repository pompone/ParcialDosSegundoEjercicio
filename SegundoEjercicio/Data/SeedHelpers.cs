using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SegundoEjercicio.Models;

namespace SegundoEjercicio.Data
{
    public static class SeedHelpers
    {
        /// <summary>
        /// Crea filas en Members para todos los usuarios que estén en el rol "Socio"
        /// y todavía no tengan su Member asociado (AppUserId).
        /// Ejecutalo 1 vez al inicio de la app.
        /// </summary>
        public static async Task BackfillMembersForSociosAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();

            var db = scope.ServiceProvider.GetRequiredService<LibraryContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

            // Trae todos los usuarios con rol "Socio"
            var socios = await userManager.GetUsersInRoleAsync("Socio");

            foreach (var u in socios)
            {
                // ¿Ya existe un Member para este usuario?
                bool existe = await db.Members.AnyAsync(m => m.AppUserId == u.Id);
                if (!existe)
                {
                    db.Members.Add(new Member
                    {
                        FullName = (u.UserName ?? u.Email ?? "Socio").Trim(),
                        Email = u.Email,
                        AppUserId = u.Id
                    });
                }
            }

            await db.SaveChangesAsync();
        }
    }
}
