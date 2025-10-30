using System.ComponentModel.DataAnnotations;

namespace SegundoEjercicio.Models
{
    public class Loan
    {
        public int Id { get; set; }

        [Display(Name = "Libro")]
        public int BookId { get; set; }
        public Book? Book { get; set; }

        [Display(Name = "Socio")]
        public int MemberId { get; set; }
        public Member? Member { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Fecha de préstamo")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = true)]
        public DateTime LoanDate { get; set; } = DateTime.Today;

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Fecha de devolución")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = true)]
        public DateTime DueDate { get; set; } = DateTime.Today.AddDays(14);

        [DataType(DataType.Date)]
        [Display(Name = "Fecha de devolución")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = true)]
        public DateTime? ReturnDate { get; set; }
    }
}
