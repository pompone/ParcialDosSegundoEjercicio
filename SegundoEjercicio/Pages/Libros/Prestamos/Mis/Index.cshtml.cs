using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SegundoEjercicio.Data;
using SegundoEjercicio.Models;

namespace SegundoEjercicio.Pages.Libros.Prestamos.Mis
{
    [Authorize(Roles = "Socio")]
    public class IndexModel : PageModel
    {
        private readonly LibraryContext _db;
        private readonly UserManager<AppUser> _userMgr;

        public IndexModel(LibraryContext db, UserManager<AppUser> userMgr)
        {
            _db = db;
            _userMgr = userMgr;
        }

        public List<(string BookTitle, DateTime RequestedAt)> Pending { get; set; } = new();
        public List<(string BookTitle, DateTime DueDate)> Active { get; set; } = new();
        public List<(string BookTitle, DateTime LoanDate, DateTime? ReturnDate)> History { get; set; } = new();

        public async Task OnGetAsync()
        {
            var user = await _userMgr.GetUserAsync(User);
            var userId = user!.Id;

            // Buscar Member por AppUserId; si no existe (usuario nuevo), crearlo
            var member = await _db.Members.SingleOrDefaultAsync(m => m.AppUserId == userId);
            if (member is null)
            {
                member = new Member
                {
                    AppUserId = userId,
                    FullName = user.FullName ?? user.Email ?? "Socio"
                    // ⬆️ Sin JoinedAt; solo lo uses si tu entidad lo tiene
                };
                _db.Members.Add(member);
                await _db.SaveChangesAsync();
            }

            var memberId = member.Id;

            Pending = await _db.LoanRequests
                .Where(r => r.MemberId == memberId && r.Status == LoanRequestStatus.Pending)
                .OrderByDescending(r => r.RequestedAt)
                .Select(r => new ValueTuple<string, DateTime>(r.Book.Title, r.RequestedAt))
                .ToListAsync();

            Active = await _db.Loans
                .Where(l => l.MemberId == memberId && l.ReturnDate == null)
                .OrderBy(l => l.DueDate)
                .Select(l => new ValueTuple<string, DateTime>(l.Book.Title, l.DueDate))
                .ToListAsync();

            History = await _db.Loans
                .Where(l => l.MemberId == memberId && l.ReturnDate != null)
                .OrderByDescending(l => l.ReturnDate)
                .Select(l => new ValueTuple<string, DateTime, DateTime?>(l.Book.Title, l.LoanDate, l.ReturnDate))
                .ToListAsync();
        }
    }
}

