using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SegundoEjercicio.Data;
using SegundoEjercicio.Models;

namespace SegundoEjercicio.Pages.Libros
{
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Bibliotecario")]
    public class EditModel : PageModel
    {
        private readonly LibraryContext _db;
        public EditModel(LibraryContext db) { _db = db; }

        [BindProperty] public Book Book { get; set; } = default!;

        // “Otros…”
        [BindProperty] public string? NewAuthorName { get; set; }
        [BindProperty] public string? NewCategoryName { get; set; }

        public SelectList Authors { get; set; } = default!;
        public SelectList Categories { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Book = await _db.Books
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == id);

            if (Book == null) return NotFound();

            Authors = new SelectList(
                await _db.Authors.OrderBy(a => a.Name).ToListAsync(),
                "Id", "Name", Book.AuthorId
            );

            Categories = new SelectList(
                await _db.Categories.OrderBy(c => c.Name).ToListAsync(),
                "Id", "Name", Book.CategoryId 
            );

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Si el usuario eligió “Otro…” y escribió un autor nuevo
            if (!string.IsNullOrWhiteSpace(NewAuthorName))
            {
                var name = NewAuthorName.Trim();
                var author = await _db.Authors.FirstOrDefaultAsync(a => a.Name == name);
                if (author == null)
                {
                    author = new Author { Name = name };
                    _db.Authors.Add(author);
                    await _db.SaveChangesAsync();
                }
                Book.AuthorId = author.Id; // forzar FK
            }

            // Si escribió una categoría nueva
            if (!string.IsNullOrWhiteSpace(NewCategoryName))
            {
                var name = NewCategoryName.Trim();
                var cat = await _db.Categories.FirstOrDefaultAsync(c => c.Name == name);
                if (cat == null)
                {
                    cat = new Category { Name = name };
                    _db.Categories.Add(cat);
                    await _db.SaveChangesAsync();
                }
                Book.CategoryId = cat.Id; // forzar FK
            }

            if (!ModelState.IsValid)
            {
                // Recontruir combos manteniendo la selección actual
                Authors = new SelectList(
                    await _db.Authors.OrderBy(a => a.Name).ToListAsync(),
                    "Id", "Name", Book.AuthorId
                );
                Categories = new SelectList(
                    await _db.Categories.OrderBy(c => c.Name).ToListAsync(),
                    "Id", "Name", Book.CategoryId
                );
                return Page();
            }

            _db.Attach(Book).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            return RedirectToPage("Index");
        }
    }
}


