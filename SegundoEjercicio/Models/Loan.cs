using System.ComponentModel.DataAnnotations;

namespace SegundoEjercicio.Models
{
    public class Loan
    {
        public int Id { get; set; }
        public int BookId { get; set; }
        public Book? Book { get; set; }
        public int MemberId { get; set; }
        public Member? Member { get; set; }

        [DataType(DataType.Date)] public DateTime LoanDate { get; set; } = DateTime.Today;
        [DataType(DataType.Date)] public DateTime DueDate { get; set; } = DateTime.Today.AddDays(14);
        [DataType(DataType.Date)] public DateTime? ReturnDate { get; set; }
    }
}
