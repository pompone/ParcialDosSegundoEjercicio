using System.ComponentModel.DataAnnotations;

namespace SegundoEjercicio.Models
{
    public class Author
    {
        public int Id { get; set; }
        [Required, StringLength(120)] public string Name { get; set; } = "";
        public ICollection<Book> Books { get; set; } = new List<Book>();
    }
}
