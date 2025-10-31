using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SegundoEjercicio.Data;
using SegundoEjercicio.Models;

namespace SegundoEjercicio.Pages.Libros
{
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Bibliotecario")]
    public class CreateModel : PageModel
    {
        private readonly LibraryContext _db;
        public CreateModel(LibraryContext db) { _db = db; }

        [BindProperty] public Book Book { get; set; } = new();

        // Texto libre cuando eligen “Otro…”
        [BindProperty] public string? NewAuthorName { get; set; }
        [BindProperty] public string? NewCategoryName { get; set; }

        public SelectList Authors { get; set; } = default!;
        public SelectList Categories { get; set; } = default!;

        // Flash message superior
        [TempData] public string? Flash { get; set; }

        public async Task OnGetAsync()
        {
            Authors = new SelectList(await _db.Authors.OrderBy(a => a.Name).ToListAsync(), "Id", "Name");
            Categories = new SelectList(await _db.Categories.OrderBy(c => c.Name).ToListAsync(), "Id", "Name");
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Detectar si usaron “Otro…” (el JS renombra los selects a Ignore_*)
            bool usedOtherAuthor = Request.Form.ContainsKey("Ignore_Book.AuthorId");
            bool usedOtherCat    = Request.Form.ContainsKey("Ignore_Book.CategoryId");

            // Si NO usaron “Otro…”, limpiamos cualquier error pegado de los textboxes
            if (!usedOtherAuthor) ModelState.Remove(nameof(NewAuthorName));
            if (!usedOtherCat)    ModelState.Remove(nameof(NewCategoryName));

            // ── Autor ─────────────────────────────────────────────
            if (usedOtherAuthor && !string.IsNullOrWhiteSpace(NewAuthorName))
            {
                var name = NewAuthorName.Trim();
                bool exists = await _db.Authors.AnyAsync(a => a.Name.ToLower() == name.ToLower());
                if (exists)
                {
                    // Mensaje explícito ARRIBA
                    ModelState.AddModelError(string.Empty, "Ese autor ya existe. Elegilo de la lista o escribí uno distinto.");
                    Flash = "El autor que escribiste ya existe. Elegilo de la lista o escribí uno distinto.";
                }
                else
                {
                    var author = new Author { Name = name };
                    _db.Authors.Add(author);
                    await _db.SaveChangesAsync();
                    Book.AuthorId = author.Id;
                }
            }

            // ── Categoría ─────────────────────────────────────────
            if (usedOtherCat && !string.IsNullOrWhiteSpace(NewCategoryName))
            {
                var name = NewCategoryName.Trim();
                bool exists = await _db.Categories.AnyAsync(c => c.Name.ToLower() == name.ToLower());
                if (exists)
                {
                    ModelState.AddModelError(string.Empty, "Esa categoría ya existe. Elegila de la lista o escribí una distinta.");
                    Flash = "La categoría que escribiste ya existe. Elegila de la lista o escribí una distinta.";
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
                Authors = new SelectList(await _db.Authors.OrderBy(a => a.Name).ToListAsync(), "Id", "Name", Book.AuthorId);
                Categories = new SelectList(await _db.Categories.OrderBy(c => c.Name).ToListAsync(), "Id", "Name", Book.CategoryId);
                return Page();
            }

            // Evitar validación sobre navegación si existiera
            ModelState.Remove("Book.Author");
            ModelState.Remove("Book.Category");

            _db.Books.Add(Book);
            await _db.SaveChangesAsync();
            return RedirectToPage("Index");
        }
    }
}



