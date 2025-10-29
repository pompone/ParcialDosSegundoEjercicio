using SegundoEjercicio.Models;

namespace SegundoEjercicio.Data
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(LibraryContext db)
        {
            if (!db.Authors.Any())
                db.Authors.AddRange(
                    new Author { Name = "Gabriel García Márquez" },
                    new Author { Name = "Isabel Allende" },
                    new Author { Name = "Jorge Luis Borges" });

            if (!db.Categories.Any())
                db.Categories.AddRange(
                    new Category { Name = "Novela" },
                    new Category { Name = "Cuento" },
                    new Category { Name = "Ensayo" });

            await db.SaveChangesAsync();

            if (!db.Books.Any())
            {
                var gabo = db.Authors.First(a => a.Name.Contains("Márquez"));
                var borges = db.Authors.First(a => a.Name.Contains("Borges"));
                var novela = db.Categories.First(c => c.Name == "Novela");
                var cuento = db.Categories.First(c => c.Name == "Cuento");

                db.Books.AddRange(
                    new Book { Title = "Cien años de soledad", AuthorId = gabo.Id, CategoryId = novela.Id, PublishedYear = 1967, ISBN = "978-0307474728", CopiesAvailable = 3 },
                    new Book { Title = "El Aleph", AuthorId = borges.Id, CategoryId = cuento.Id, PublishedYear = 1949, ISBN = "978-9875668100", CopiesAvailable = 2 });
            }

            if (!db.Members.Any())
                db.Members.AddRange(
                    new Member { FullName = "Ana Pérez", Email = "ana@example.com" },
                    new Member { FullName = "Luis Díaz", Email = "luis@example.com" });

            await db.SaveChangesAsync();
        }
    }
}
