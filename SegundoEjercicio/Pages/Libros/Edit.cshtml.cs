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

        public SelectList Authors { get; set; } = default!;
        public SelectList Categories { get; set; } = default!;

        [TempData] public string? Flash { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Book = await _db.Books.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id);
            if (Book == null) return NotFound();

            Authors = new SelectList(await _db.Authors.OrderBy(a => a.Name).ToListAsync(), "Id", "Name", Book.AuthorId);
            Categories = new SelectList(await _db.Categories.OrderBy(c => c.Name).ToListAsync(), "Id", "Name", Book.CategoryId);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            bool usedOtherAuthor = Request.Form.ContainsKey("Ignore_Book.AuthorId");
            bool usedOtherCat    = Request.Form.ContainsKey("Ignore_Book.CategoryId");

            if (!usedOtherAuthor) ModelState.Remove(nameof(NewAuthorName));
            if (!usedOtherCat)    ModelState.Remove(nameof(NewCategoryName));

            if (usedOtherAuthor && !string.IsNullOrWhiteSpace(NewAuthorName))
            {
                var name = NewAuthorName.Trim();
                bool exists = await _db.Authors.AnyAsync(a => a.Name.ToLower() == name.ToLower());
                if (exists)
                {
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

            _db.Attach(Book).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            return RedirectToPage("Index");
        }
    }
}






