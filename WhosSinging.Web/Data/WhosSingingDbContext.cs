using Microsoft.EntityFrameworkCore;
using WhosSinging.Web.Models;

namespace WhosSinging.Web.Data;

public class WhosSingingDbContext : DbContext
{
    public WhosSingingDbContext(DbContextOptions<WhosSingingDbContext> options)
        : base(options)
    {
    }

    public DbSet<Song> Songs => Set<Song>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Song>(entity =>
        {
            entity.HasIndex(s => s.titulo);
            entity.HasIndex(s => s.artista);
            entity.HasIndex(s => s.genero);

            // youtube_id único: evita duplicados al re-importar
            entity.HasIndex(s => s.youtube_id)
                  .IsUnique();
        });
    }
}