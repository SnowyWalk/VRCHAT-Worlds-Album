using Microsoft.EntityFrameworkCore;
using server.Schema;
namespace server.Service;

public class DB : DbContext
{
    public DbSet<WorldData> Data => Set<WorldData>();
    public DbSet<WorldImage> Image => Set<WorldImage>();
    public DbSet<WorldMetadata> Metadata => Set<WorldMetadata>();
    public DbSet<WorldCategory> Category => Set<WorldCategory>();
    public DbSet<WorldDescription> Description => Set<WorldDescription>();

    public DB(DbContextOptions<DB> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WorldData>(e =>
        {
            e.Property(x => x.WorldId).IsRequired().HasMaxLength(42);
            e.HasKey(x => x.WorldId);

            e.HasOne(x => x.Metadata)
                .WithOne(y => y.WorldData)
                .HasForeignKey<WorldMetadata>(z => z.WorldId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasMany(e => e.CategoryList)
                .WithMany(x => x.WorldDataList);

            e.HasOne(x => x.Description)
                .WithOne(x => x.WorldData)
                .HasForeignKey<WorldDescription>(x => x.WorldId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(x => x.WorldId).IsUnique();
            e.HasIndex(x => x.DataCreatedAt);
            e.HasIndex(x => new { x.DataCreatedAt, x.WorldId }).IsDescending(true, false); // 첫 컬럼만 DESC
        });

        modelBuilder.Entity<WorldImage>(e =>
        {
            e.HasKey(x => new { x.WorldId, x.Filename }); // 복합 PK (월드 내 파일명 유니크)

            e.Property(x => x.WorldId).IsRequired().HasMaxLength(42);
            e.Property(x => x.Filename).IsRequired();
            e.Property(x => x.Width);
            e.Property(x => x.Height);

            e.HasIndex(x => x.WorldId);
            e.HasIndex(x => x.Filename);

            e.HasOne(x => x.WorldData) // FK → WorldData.WorldId
                .WithMany(y => y.ImageList)
                .HasForeignKey(z => z.WorldId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WorldMetadata>(e =>
        {
            e.HasKey(x => x.WorldId); // 복합 PK (월드 내 파일명 유니크)

            e.Property(x => x.WorldId).IsRequired().HasMaxLength(42);
            e.Property(x => x.WorldName);
            e.Property(x => x.AuthorId).HasMaxLength(42);
            e.Property(x => x.AuthorName);
            e.Property(x => x.ImageUrl);
            e.Property(x => x.Capacity);
            e.Property(x => x.Visits);
            e.Property(x => x.Favorites);
            e.Property(x => x.Heat);
            e.Property(x => x.Popularity);
            e.Property(x => x.Tags);
            e.Property(x => x.UpdatedAt).IsConcurrencyToken();

            e.HasIndex(x => x.WorldId).IsUnique();
        });

        modelBuilder.Entity<WorldCategory>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired();
            e.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<WorldDescription>(e =>
        {
            e.HasKey(x => x.WorldId);
            e.Property(x => x.WorldId).IsRequired().HasMaxLength(42);
            e.Property(x => x.Description).IsRequired();
            e.HasIndex(x => x.WorldId).IsUnique();
        });
    }
}