using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SegundoEjercicio.Data;
using SegundoEjercicio.Models;

namespace SegundoEjercicio.Pages.Libros.Solicitudes
{
    [Authorize(Roles = "Bibliotecario")]
    public class IndexModel : PageModel
    {
        private readonly LibraryContext _db;
        public IndexModel(LibraryContext db) => _db = db;

        public record Row(int Id, DateTime RequestedAt, string MemberName, string BookTitle, DateTime? DesiredReturnDate);
        public List<Row> Rows { get; set; } = new();

        public async Task OnGetAsync()
        {
            var list = await _db.LoanRequests
                .Where(r => r.Status == LoanRequestStatus.Pending)
                .OrderByDescending(r => r.RequestedAt)
                .Select(r => new
                {
                    r.Id,
                    r.RequestedAt,
                    r.Notes,
                    MemberName = r.Member.FullName,
                    BookTitle = r.Book.Title
                })
                .ToListAsync();

            Rows = list.Select(x => new Row(
                x.Id,
                x.RequestedAt,
                x.MemberName,
                x.BookTitle,
                ParseDesiredReturnDate(x.Notes)
            )).ToList();
        }

        // ====== Approve / Reject ======

        public async Task<IActionResult> OnPostApproveAsync(int id)
        {
            var req = await _db.LoanRequests
                .Include(r => r.Book)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (req is null) { TempData["msg"] = "Solicitud no encontrada."; return RedirectToPage(); }
            if (req.Status != LoanRequestStatus.Pending) { TempData["msg"] = "La solicitud ya fue procesada."; return RedirectToPage(); }

          
            var book = req.Book!;
            if (book.CopiesAvailable <= 0)
            {
                TempData["msg"] = "No hay ejemplares disponibles para aprobar esta solicitud.";
                return RedirectToPage();
            }

            // Descontar 1 del stock al aprobar
            book.CopiesAvailable--;

            var desired = ParseDesiredReturnDate(req.Notes);
            var loanDate = DateTime.UtcNow;
            var dueDate = desired?.Date.AddHours(23).AddMinutes(59) ?? loanDate.AddDays(14);

            _db.Loans.Add(new Loan
            {
                BookId = req.BookId,
                MemberId = req.MemberId,
                LoanDate = loanDate,
                DueDate = dueDate,
                ReturnDate = null
            });

            req.Status = LoanRequestStatus.Approved;

            await _db.SaveChangesAsync();
            TempData["msg"] = "Solicitud aprobada, préstamo creado y stock actualizado.";
            return RedirectToPage();
        }


        // ====== Helper: parsear fecha de Notes (yyyy-MM-dd) ======
        private static DateTime? ParseDesiredReturnDate(string? notes)
        {
            if (string.IsNullOrWhiteSpace(notes)) return null;
            var m = Regex.Match(notes, @"\b\d{4}-\d{2}-\d{2}\b");
            if (!m.Success) return null;

            return DateTime.TryParseExact(
                m.Value, "yyyy-MM-dd",
                CultureInfo.InvariantCulture, DateTimeStyles.None,
                out var date)
                ? date
                : null;
        }
    }
}
