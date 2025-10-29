using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SegundoEjercicio.Data;
using SegundoEjercicio.Models;

namespace SegundoEjercicio.Pages.Libros
{
    public class IndexModel : PageModel
    {
        private readonly LibraryContext _db;
        public IndexModel(LibraryContext db) { _db = db; }

        public IList<Book> Books { get; set; } = new List<Book>();

        // ?? Soportan GET para bindear desde la querystring
        [BindProperty(SupportsGet = true)] public string? Q { get; set; }
        [BindProperty(SupportsGet = true)] public int? CategoryId { get; set; }

        public SelectList Categories { get; set; } = default!;

        public async Task OnGetAsync()
        {
            // Combo de categorías
            Categories = new SelectList(
                await _db.Categories.OrderBy(c => c.Name).ToListAsync(),
                "Id", "Name", CategoryId
            );

            // Query base
            var query = _db.Books
                .Include(b => b.Author)
                .Include(b => b.Category)
                .AsQueryable();

            // Filtro por categoría (solo si hay valor)
            if (CategoryId.HasValue)
                query = query.Where(b => b.CategoryId == CategoryId.Value);

            // Búsqueda (usa LIKE para que MySQL lo haga en DB y sea case-insensitive con collation)
            if (!string.IsNullOrWhiteSpace(Q))
            {
                var p = $"%{Q}%";
                query = query.Where(b =>
                    EF.Functions.Like(b.Title, p) ||
                    EF.Functions.Like(b.Author!.Name, p) ||
                    EF.Functions.Like(b.Category!.Name, p) ||
                    (b.ISBN != null && EF.Functions.Like(b.ISBN, p))
                );
            }

            Books = await query.OrderBy(b => b.Title).ToListAsync();
        }
    }
}
