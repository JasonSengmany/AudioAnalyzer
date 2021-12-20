using Microsoft.EntityFrameworkCore;

namespace AudioAnalyzer.Services;

public class SongDbContext : DbContext
{
    public SongDbContext() { }
    public SongDbContext(DbContextOptions<SongDbContext> options) : base(options) { }

    // Uncomment when adding migration
    // protected override void OnConfiguring(DbContextOptionsBuilder builder)
    // {
    //     builder.UseSqlite("Data Source=songs.db");
    //     base.OnConfiguring(builder);
    // }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Song>()
            .HasKey(s => s.FilePath);
        modelBuilder.Entity<Song>()
            .Ignore(s => s.MFCC);
    }
    public DbSet<Song> Songs { get; set; }
}
