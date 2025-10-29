using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SegundoEjercicio.Data;
using SegundoEjercicio.Models;

namespace SegundoEjercicio.Pages.Libros.Solicitudes
{
    [Authorize(Roles = "Socio")]
    public class CreateModel : PageModel
    {
        private readonly LibraryContext _db;
        private readonly UserManager<AppUser> _userMgr;

        public CreateModel(LibraryContext db, UserManager<AppUser> userMgr)
        {
            _db = db; _userMgr = userMgr;
        }

        [BindProperty] public int BookId { get; set; }

        [Display(Name = "Fecha de devolución")]
        [DataType(DataType.Date)]
        [Required(ErrorMessage = "Elegí una fecha de devolución.")]
        [BindProperty] public DateTime? DesiredReturnDate { get; set; }

        public Book? Book { get; set; }
        public string? Error { get; set; }

        public async Task<IActionResult> OnGetAsync(int bookId)
        {
            BookId = bookId;
            Book = await _db.Books.AsNoTracking().FirstOrDefaultAsync(b => b.Id == bookId);
            if (Book == null) return NotFound();

            // Valor por defecto: mañana
            DesiredReturnDate ??= DateTime.Today.AddDays(1);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Validación manual de rango (opcional: 1 a 30 días)
            if (!ModelState.IsValid ||
                DesiredReturnDate == null ||
                DesiredReturnDate < DateTime.Today.AddDays(1) ||
                DesiredReturnDate > DateTime.Today.AddDays(30))
            {
                if (DesiredReturnDate is not null)
                {
                    if (DesiredReturnDate < DateTime.Today.AddDays(1))
                        ModelState.AddModelError(nameof(DesiredReturnDate), "La fecha debe ser a partir de mañana.");
                    if (DesiredReturnDate > DateTime.Today.AddDays(30))
                        ModelState.AddModelError(nameof(DesiredReturnDate), "La fecha no puede superar 30 días.");
                }

                Book = await _db.Books.AsNoTracking().FirstOrDefaultAsync(b => b.Id == BookId);
                return Page();
            }

            var user = await _userMgr.GetUserAsync(User);
            if (user == null) return Challenge();

            // Buscar el Member asociado al usuario logueado
            var member = await _db.Members.FirstOrDefaultAsync(m => m.AppUserId == user.Id);
            if (member == null)
            {
                Error = "No se encontró tu perfil de socio. Contactá al bibliotecario.";
                Book = await _db.Books.AsNoTracking().FirstOrDefaultAsync(b => b.Id == BookId);
                return Page();
            }

            // ¿Ya hay solicitud pendiente de este libro?
            bool yaPendiente = await _db.LoanRequests
                .AnyAsync(r => r.MemberId == member.Id &&
                               r.BookId == BookId &&
                               r.Status == LoanRequestStatus.Pending);
            if (yaPendiente)
            {
                Error = "Ya tenés una solicitud pendiente para este libro.";
                Book = await _db.Books.AsNoTracking().FirstOrDefaultAsync(b => b.Id == BookId);
                return Page();
            }

            // ¿Ya tiene préstamo ACTIVO del libro?
            bool prestamoActivo = await _db.Loans
                .AnyAsync(p => p.MemberId == member.Id &&
                               p.BookId == BookId &&
                               p.ReturnDate == null);
            if (prestamoActivo)
            {
                Error = "Ya tenés este libro en préstamo.";
                Book = await _db.Books.AsNoTracking().FirstOrDefaultAsync(b => b.Id == BookId);
                return Page();
            }

            // Crear solicitud (guardamos la fecha en Notes para no tocar modelo/BD)
            _db.LoanRequests.Add(new LoanRequest
            {
                BookId = BookId,
                MemberId = member.Id,
                Notes = $"Fecha solicitada de devolución: {DesiredReturnDate:yyyy-MM-dd}",
                Status = LoanRequestStatus.Pending
            });

            await _db.SaveChangesAsync();

            return RedirectToPage("/Libros/Prestamos/Mis/Index");
        }
    }
}
