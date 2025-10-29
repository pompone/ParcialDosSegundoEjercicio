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

        [Display(Name = "Fecha de devoluci�n")]
        [DataType(DataType.Date)]
        [Required(ErrorMessage = "Eleg� una fecha de devoluci�n.")]
        [BindProperty] public DateTime? DesiredReturnDate { get; set; }

        public Book? Book { get; set; }
        public string? Error { get; set; }

        public async Task<IActionResult> OnGetAsync(int bookId)
        {
            BookId = bookId;
            Book = await _db.Books.AsNoTracking().FirstOrDefaultAsync(b => b.Id == bookId);
            if (Book == null) return NotFound();

            // Valor por defecto: ma�ana
            DesiredReturnDate ??= DateTime.Today.AddDays(1);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Validaci�n manual de rango (opcional: 1 a 30 d�as)
            if (!ModelState.IsValid ||
                DesiredReturnDate == null ||
                DesiredReturnDate < DateTime.Today.AddDays(1) ||
                DesiredReturnDate > DateTime.Today.AddDays(30))
            {
                if (DesiredReturnDate is not null)
                {
                    if (DesiredReturnDate < DateTime.Today.AddDays(1))
                        ModelState.AddModelError(nameof(DesiredReturnDate), "La fecha debe ser a partir de ma�ana.");
                    if (DesiredReturnDate > DateTime.Today.AddDays(30))
                        ModelState.AddModelError(nameof(DesiredReturnDate), "La fecha no puede superar 30 d�as.");
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
                Error = "No se encontr� tu perfil de socio. Contact� al bibliotecario.";
                Book = await _db.Books.AsNoTracking().FirstOrDefaultAsync(b => b.Id == BookId);
                return Page();
            }

            // �Ya hay solicitud pendiente de este libro?
            bool yaPendiente = await _db.LoanRequests
                .AnyAsync(r => r.MemberId == member.Id &&
                               r.BookId == BookId &&
                               r.Status == LoanRequestStatus.Pending);
            if (yaPendiente)
            {
                Error = "Ya ten�s una solicitud pendiente para este libro.";
                Book = await _db.Books.AsNoTracking().FirstOrDefaultAsync(b => b.Id == BookId);
                return Page();
            }

            // �Ya tiene pr�stamo ACTIVO del libro?
            bool prestamoActivo = await _db.Loans
                .AnyAsync(p => p.MemberId == member.Id &&
                               p.BookId == BookId &&
                               p.ReturnDate == null);
            if (prestamoActivo)
            {
                Error = "Ya ten�s este libro en pr�stamo.";
                Book = await _db.Books.AsNoTracking().FirstOrDefaultAsync(b => b.Id == BookId);
                return Page();
            }

            // Crear solicitud (guardamos la fecha en Notes para no tocar modelo/BD)
            _db.LoanRequests.Add(new LoanRequest
            {
                BookId = BookId,
                MemberId = member.Id,
                Notes = $"Fecha solicitada de devoluci�n: {DesiredReturnDate:yyyy-MM-dd}",
                Status = LoanRequestStatus.Pending
            });

            await _db.SaveChangesAsync();

            return RedirectToPage("/Libros/Prestamos/Mis/Index");
        }
    }
}
