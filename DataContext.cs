using Microsoft.EntityFrameworkCore;

namespace EfCoreBug;

public class DataContext : DbContext
{
    public DbSet<ParentModel> Parents { get; set; }

    public DbSet<ChildModel> Children { get; set; }

    public DataContext(DbContextOptions<DataContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ParentModel>()
            .HasMany(p => p.Children);

        // We configure the complex property to be required. We want it to never be null.
        modelBuilder.Entity<ChildModel>()
            .ComplexProperty(c => c.Complex)
            .IsRequired();
    }
}
