using Microsoft.AspNetCore.Mvc;                      // TempData, IActionResult
using Microsoft.AspNetCore.Mvc.RazorPages;           // PageModel
using Microsoft.EntityFrameworkCore;                 // EF Core
using SegundoEjercicio.Data;                          // LibraryContext
using SegundoEjercicio.Models;                        // Loan, Book

namespace SegundoEjercicio.Pages.Prestamos
{
    public class IndexModel : PageModel
    {
        private readonly LibraryContext _db;
        public IndexModel(LibraryContext db) { _db = db; }

        public IList<Loan> Loans { get; set; } = new List<Loan>();

        // Mensaje flash (cartel amarillo en la vista)
        [TempData] public string? Flash { get; set; }

        public async Task OnGetAsync()
        {
            Loans = await _db.Loans
                .Include(l => l.Book).ThenInclude(b => b.Author)
                .Include(l => l.Member)
                .OrderByDescending(l => l.LoanDate)
                .ToListAsync();
        }

        // Catch-all por si llega un POST sin handler → volver al GET y recargar
        public IActionResult OnPost() => RedirectToPage("./Index");

        // Marcar como devuelto (repone stock) y mostrar mensaje
        public async Task<IActionResult> OnPostReturnAsync(int id)
        {
            var loan = await _db.Loans
                .Include(l => l.Book)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (loan == null)
            {
                Flash = "El préstamo ya no existe.";
                return RedirectToPage("./Index");
            }

            if (loan.ReturnDate == null)
            {
                loan.ReturnDate = DateTime.Today;
                if (loan.Book != null)
                    loan.Book.CopiesAvailable += 1;

                await _db.SaveChangesAsync();
                Flash = "Préstamo marcado como devuelto.";
            }

            return RedirectToPage("./Index");
        }

        // Eliminar: solo permitido si YA fue devuelto; caso contrario, aviso
        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var loan = await _db.Loans.FirstOrDefaultAsync(l => l.Id == id);

            if (loan == null)
            {
                Flash = "El préstamo ya no existe.";
                return RedirectToPage("./Index");
            }

            if (loan.ReturnDate == null)
            {
                Flash = "No se puede eliminar: el libro no fue devuelto.";
                return RedirectToPage("./Index");
            }

            _db.Loans.Remove(loan);
            await _db.SaveChangesAsync();
            Flash = "Préstamo eliminado.";

            return RedirectToPage("./Index");
        }
    }
}
