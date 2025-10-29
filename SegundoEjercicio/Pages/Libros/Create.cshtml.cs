using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SegundoEjercicio.Data;
using SegundoEjercicio.Models;

namespace SegundoEjercicio.Pages.Libros
{
    public class CreateModel : PageModel
    {
        private readonly LibraryContext _db;
        public CreateModel(LibraryContext db) { _db = db; }

        [BindProperty] public Book Book { get; set; } = new();

        // “Otros…”
        [BindProperty] public string? NewAuthorName { get; set; }
        [BindProperty] public string? NewCategoryName { get; set; }

        public SelectList Authors { get; set; } = default!;
        public SelectList Categories { get; set; } = default!;

        public async Task OnGetAsync() => await LoadCombosAsync();

        public async Task<IActionResult> OnPostAsync()
        {
            // Si el usuario tipeó un autor nuevo, lo creo/uso
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
                Book.AuthorId = author.Id; // forzamos el nuevo autor
            }

            // Si tipeó una categoría nueva, la creo/uso
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
                Book.CategoryId = cat.Id;
            }

            if (!ModelState.IsValid)
            {
                await LoadCombosAsync();
                return Page();
            }

            _db.Books.Add(Book);
            await _db.SaveChangesAsync();
            return RedirectToPage("Index");
        }

        private async Task LoadCombosAsync()
        {
            Authors = new SelectList(await _db.Authors.OrderBy(a => a.Name).ToListAsync(), "Id", "Name");
            Categories = new SelectList(await _db.Categories.OrderBy(c => c.Name).ToListAsync(), "Id", "Name");
        }
    }
}

