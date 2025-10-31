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

        [BindProperty] public string? NewAuthorName { get; set; }
        [BindProperty] public string? NewCategoryName { get; set; }

        // Para mantener “Otro…” visible tras error
        [BindProperty] public bool ForceOtherAuthor { get; set; }
        [BindProperty] public bool ForceOtherCategory { get; set; }

        public SelectList Authors { get; set; } = default!;
        public SelectList Categories { get; set; } = default!;

        [TempData] public string? Flash { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var book = await _db.Books
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == id);

            if (book is null) return NotFound();

            Book = book;
            if (Book == null) return NotFound();

            Authors = new SelectList(await _db.Authors.OrderBy(a => a.Name).ToListAsync(), "Id", "Name", Book.AuthorId);
            Categories = new SelectList(await _db.Categories.OrderBy(c => c.Name).ToListAsync(), "Id", "Name", Book.CategoryId);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            bool usedOtherAuthor = Request.Form.ContainsKey("Ignore_Book.AuthorId");
            bool usedOtherCat = Request.Form.ContainsKey("Ignore_Book.CategoryId");

            ForceOtherAuthor = usedOtherAuthor;
            ForceOtherCategory = usedOtherCat;

            if (!usedOtherAuthor) ModelState.Remove(nameof(NewAuthorName));
            if (!usedOtherCat) ModelState.Remove(nameof(NewCategoryName));

            // Autor
            if (usedOtherAuthor)
            {
                if (string.IsNullOrWhiteSpace(NewAuthorName))
                {
                    ModelState.AddModelError(nameof(NewAuthorName), "Ingresá el nombre del autor.");
                }
                else
                {
                    var name = NewAuthorName.Trim();
                    bool exists = await _db.Authors.AnyAsync(a => a.Name.ToLower() == name.ToLower());
                    if (exists)
                    {
                        ModelState.AddModelError(nameof(NewAuthorName), "Ese autor ya existe. Elegilo de la lista o escribí uno distinto.");
                    }
                    else
                    {
                        var author = new Author { Name = name };
                        _db.Authors.Add(author);
                        await _db.SaveChangesAsync();
                        Book.AuthorId = author.Id;
                    }
                }
            }

            // Categoría
            if (usedOtherCat)
            {
                if (string.IsNullOrWhiteSpace(NewCategoryName))
                {
                    ModelState.AddModelError(nameof(NewCategoryName), "Ingresá el nombre de la categoría.");
                }
                else
                {
                    var name = NewCategoryName.Trim();
                    bool exists = await _db.Categories.AnyAsync(c => c.Name.ToLower() == name.ToLower());
                    if (exists)
                    {
                        ModelState.AddModelError(nameof(NewCategoryName), "Esa categoría ya existe. Elegila de la lista o escribí una distinta.");
                    }
                    else
                    {
                        var cat = new Category { Name = name };
                        _db.Categories.Add(cat);
                        await _db.SaveChangesAsync();
                        Book.CategoryId = cat.Id;
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                Authors = new SelectList(await _db.Authors.OrderBy(a => a.Name).ToListAsync(), "Id", "Name", Book.AuthorId);
                Categories = new SelectList(await _db.Categories.OrderBy(c => c.Name).ToListAsync(), "Id", "Name", Book.CategoryId);
                return Page();
            }

            _db.Attach(Book).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            TempData["Flash"] = "Libro actualizado correctamente.";
            return RedirectToPage("Index");
        }
    }
}





