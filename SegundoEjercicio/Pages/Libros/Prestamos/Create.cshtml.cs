using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SegundoEjercicio.Data;
using SegundoEjercicio.Models;

namespace SegundoEjercicio.Pages.Prestamos
{
    public class CreateModel : PageModel
    {
        private readonly LibraryContext _db;
        public CreateModel(LibraryContext db) { _db = db; }

        [BindProperty]
        public Loan Loan { get; set; } = new() { LoanDate = DateTime.Today, DueDate = DateTime.Today.AddDays(14) };

        public SelectList Books { get; set; } = default!;
        public SelectList Members { get; set; } = default!;

        public async Task OnGetAsync(int? bookId)
        {
            var books = await _db.Books.Where(b => b.CopiesAvailable > 0).OrderBy(b => b.Title).ToListAsync();
            Books = new SelectList(books, "Id", "Title", bookId);
            Members = new SelectList(await _db.Members.OrderBy(m => m.FullName).ToListAsync(), "Id", "FullName");
            if (bookId.HasValue) Loan.BookId = bookId.Value;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var book = await _db.Books.FindAsync(Loan.BookId);
            if (book == null || book.CopiesAvailable <= 0)
            {
                ModelState.AddModelError("", "El libro no está disponible.");
                await OnGetAsync(null);
                return Page();
            }

            book.CopiesAvailable -= 1;
            _db.Loans.Add(Loan);
            await _db.SaveChangesAsync();
            return RedirectToPage("Index");
        }
    }
}
