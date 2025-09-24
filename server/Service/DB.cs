using Microsoft.EntityFrameworkCore;
using server.Schema;
namespace server.Service;

public class DB : DbContext
{
    public DbSet<WorldData> Data => Set<WorldData>();

    public DB(DbContextOptions<DB> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WorldData>(e =>
        {
            e.Property(x => x.WorldId).IsRequired().HasMaxLength(42);

            e.OwnsOne(x => x.Metadata);
            e.OwnsOne(x => x.Category);
            e.OwnsOne(x => x.Description);

            // ----- 컬렉션 Owned (별도 테이블) -----
            e.OwnsMany(x => x.ImageList, nb =>
            {
                nb.ToTable("WorldImages");               // 테이블명
                nb.WithOwner().HasForeignKey("WorldId"); // FK(소유자 키)
                nb.HasKey("WorldId", "Filename");        // 월드 내 파일명 유니크

                nb.Property(p => p.WorldId).HasColumnName("WorldId");
                nb.Property(p => p.Filename).HasColumnName("Filename");
                nb.Property(p => p.Width).HasColumnName("Width");
                nb.Property(p => p.Height).HasColumnName("Height");

                nb.HasIndex("WorldId");
                nb.HasIndex("Filename");
            });
            e.Navigation(x => x.ImageList).AutoInclude();

            e.HasIndex(x => x.WorldId).IsUnique();
            e.HasIndex(x => x.DataCreatedAt);
            e.HasIndex(x => new { x.DataCreatedAt, x.WorldId }).IsDescending(true, false); // 첫 컬럼만 DESC
        });
    }
}