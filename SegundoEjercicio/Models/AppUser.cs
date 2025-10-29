using Microsoft.AspNetCore.Identity;

namespace SegundoEjercicio.Models
{
    public class AppUser : IdentityUser
    {
        public string? FullName { get; set; } 
    }
}
