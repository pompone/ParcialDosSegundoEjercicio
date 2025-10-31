using System.ComponentModel.DataAnnotations;

namespace SegundoEjercicio.Models
{
    public class Book
    {
        public int Id { get; set; }

        [Required, StringLength(180)]
        [Display(Name = "Título")]
        public string Title { get; set; } = "";

        [Display(Name = "Autor")]
        public int AuthorId { get; set; }
        public Author? Author { get; set; }

        [Display(Name = "Categoría")]
        public int CategoryId { get; set; }
        public Category? Category { get; set; }

        [Display(Name = "Año de publicación")]
        [Range(1000, 2100, ErrorMessage = "Ingrese un año válido.")]
        public int? PublishedYear { get; set; }

        [StringLength(32)]
        [Display(Name = "ISBN")]
        public string? ISBN { get; set; }

        [Display(Name = "Ejemplares disponibles")]
        [Range(0, int.MaxValue, ErrorMessage = "No puede ser negativo.")]
        public int CopiesAvailable { get; set; } = 1;

        [Display(Name = "Préstamos")]
        public ICollection<Loan> Loans { get; set; } = new List<Loan>();
    }
}


