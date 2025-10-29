using System.ComponentModel.DataAnnotations;

namespace SegundoEjercicio.Models
{
    public class Member
    {
        public int Id { get; set; }

        [Required, StringLength(120)]
        public string FullName { get; set; } = "";

        [EmailAddress]
        public string? Email { get; set; }

        // NUEVO: id del usuario (AspNetUsers.Id)
        public string? AppUserId { get; set; }

        public ICollection<Loan> Loans { get; set; } = new List<Loan>();
    }
}
