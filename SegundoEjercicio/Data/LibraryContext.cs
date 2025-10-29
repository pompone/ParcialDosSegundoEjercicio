using Microsoft.EntityFrameworkCore;
using SegundoEjercicio.Models;

namespace SegundoEjercicio.Data
{
    public class LibraryContext : DbContext
    {
        public LibraryContext(DbContextOptions<LibraryContext> options) : base(options) { }

        public DbSet<Author> Authors => Set<Author>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Book> Books => Set<Book>();
        public DbSet<Member> Members => Set<Member>();
        public DbSet<Loan> Loans => Set<Loan>();
        public DbSet<LoanRequest> LoanRequests => Set<LoanRequest>();
    }
}
