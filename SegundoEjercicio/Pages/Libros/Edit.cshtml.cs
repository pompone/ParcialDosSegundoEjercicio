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

        // Texto libre cuando eligen “Otro…”
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
            // ── Autor: si escribieron “Otro…”, validar duplicado y crear si no existe
            if (!string.IsNullOrWhiteSpace(NewAuthorName))
            {
                var name = NewAuthorName.Trim();

                bool exists = await _db.Authors
                    .AnyAsync(a => a.Name.ToLower() == name.ToLower());

                if (exists)
                {
                    ModelState.AddModelError("NewAuthorName",
                        "Ese autor ya existe. Elegilo de la lista.");
                }
                else
                {
                    var author = new Author { Name = name };
                    _db.Authors.Add(author);
                    await _db.SaveChangesAsync();
                    Book.AuthorId = author.Id; // usar el nuevo
                }
            }

            // ── Categoría: igual
            if (!string.IsNullOrWhiteSpace(NewCategoryName))
            {
                var name = NewCategoryName.Trim();

                bool exists = await _db.Categories
                    .AnyAsync(c => c.Name.ToLower() == name.ToLower());

                if (exists)
                {
                    ModelState.AddModelError("NewCategoryName",
                        "Esa categoría ya existe. Elegila de la lista.");
                }
                else
                {
                    var cat = new Category { Name = name };
                    _db.Categories.Add(cat);
                    await _db.SaveChangesAsync();
                    Book.CategoryId = cat.Id;
                }
            }

            if (!ModelState.IsValid)
            {
                // Repoblar combos manteniendo selección actual
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

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _db.Books.AnyAsync(b => b.Id == Book.Id))
                    return NotFound();
                throw;
            }

            return RedirectToPage("Index");
        }
    }
}



