using System.ComponentModel.DataAnnotations;

namespace SegundoEjercicio.Models
{
    public class Loan
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Libro")]
        public int BookId { get; set; }
        public Book? Book { get; set; }

        [Required]
        [Display(Name = "Socio")]
        public int MemberId { get; set; }
        public Member? Member { get; set; }

        [Display(Name = "Fecha de préstamo")]
        [DataType(DataType.Date)]
        public DateTime LoanDate { get; set; } = DateTime.Today;

        [Display(Name = "Fecha de vencimiento")]
        [DataType(DataType.Date)]
        public DateTime DueDate { get; set; } = DateTime.Today.AddDays(14);

        [Display(Name = "Fecha de devolución")]
        [DataType(DataType.Date)]
        public DateTime? ReturnDate { get; set; }
    }
}

