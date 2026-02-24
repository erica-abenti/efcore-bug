using Microsoft.EntityFrameworkCore;

namespace EfCoreBug;

public class DataContext : DbContext
{
    public DbSet<ParentModel> Parents { get; set; }

    public DataContext(DbContextOptions<DataContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ParentModel>()
            .HasMany(p => p.Children);

        modelBuilder.Entity<ChildModel>()
            .ComplexProperty(c => c.Complex)
            .IsRequired();
    }
}
