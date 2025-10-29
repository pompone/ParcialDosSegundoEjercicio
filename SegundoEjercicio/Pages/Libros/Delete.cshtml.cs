using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SegundoEjercicio.Data;
using SegundoEjercicio.Models;

namespace SegundoEjercicio.Pages.Libros
{
    public class DeleteModel : PageModel
    {
        private readonly LibraryContext _db;
        public DeleteModel(LibraryContext db) { _db = db; }

        public Book? Book { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Book = await _db.Books.Include(b => b.Author).Include(b => b.Category)
                                  .FirstOrDefaultAsync(b => b.Id == id);
            return Book == null ? NotFound() : Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            var b = await _db.Books.FindAsync(id);
            if (b != null) { _db.Books.Remove(b); await _db.SaveChangesAsync(); }
            return RedirectToPage("Index");
        }
    }
}
