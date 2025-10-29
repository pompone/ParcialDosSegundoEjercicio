using System;
using System.ComponentModel.DataAnnotations;

namespace SegundoEjercicio.Models
{
    public enum LoanRequestStatus
    {
        Pending = 0,
        Approved = 1,
        Denied = 2
    }

    public class LoanRequest
    {
        public int Id { get; set; }

        // Libro solicitado
        [Required]
        public int BookId { get; set; }
        public Book Book { get; set; } = null!;

        // Socio que solicita
        [Required]
        public int MemberId { get; set; }
        public Member Member { get; set; } = null!;

        // Fechas y estado
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public LoanRequestStatus Status { get; set; } = LoanRequestStatus.Pending;

        // Decisión del bibliotecario (solo guardamos el Id del usuario de Identity; sin FK)
        public string? DecisionByUserId { get; set; }
        public DateTime? DecisionAt { get; set; }

        // Comentarios (del socio o del bibliotecario)
        [StringLength(500)]
        public string? Notes { get; set; }
    }
}
