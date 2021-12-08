using Microsoft.EntityFrameworkCore;

namespace AudioAnalyzer.Services;
public class SongDbContext : DbContext
{
    public SongDbContext() { }
    public SongDbContext(DbContextOptions<SongDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Song>()
            .HasKey(s => s.FilePath);
        modelBuilder.Entity<Song>()
            .Ignore(s => s.MFCC);
    }
    public DbSet<Song> Songs { get; set; }
}

public class SqlitePersistenceService : IPersistenceService
{
    private SongDbContext _songDb;
    public SqlitePersistenceService(SongDbContext dbContext)
    {
        _songDb = dbContext;
    }

    public async Task Append(List<Song> songs, string path)
    {
        await _songDb.Songs.AddRangeAsync(songs);
        await _songDb.SaveChangesAsync();
    }

    public async Task<List<Song>> Load(string path)
    {
        return await _songDb.Songs.ToListAsync();
    }

    /// <summary>
    /// Truncates the Songs table and writes in the new results.
    /// </summary>
    /// <param name="songs"></param>
    /// <param name="path"></param>
    /// <returns></returns>
    public async Task Save(List<Song> songs, string path)
    {
        await _songDb.Database.ExecuteSqlRawAsync("DELETE FROM SONGS");
        await _songDb.Songs.AddRangeAsync(songs);
        await _songDb.SaveChangesAsync();
    }
}