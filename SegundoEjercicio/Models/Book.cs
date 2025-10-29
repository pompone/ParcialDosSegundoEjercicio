using System.ComponentModel.DataAnnotations;

namespace SegundoEjercicio.Models
{
    public class Book
    {
        public int Id { get; set; }
        [Required, StringLength(180)] public string Title { get; set; } = "";
        public int AuthorId { get; set; }
        public Author? Author { get; set; }
        public int CategoryId { get; set; }
        public Category? Category { get; set; }
        public int? PublishedYear { get; set; }
        [StringLength(32)] public string? ISBN { get; set; }
        public int CopiesAvailable { get; set; } = 1;
        public ICollection<Loan> Loans { get; set; } = new List<Loan>();
    }
}
