using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EfCoreBug;

public class Tests : IAsyncLifetime
{
    public const string Connection = "Host=localhost;Database=test_db;Port=;Username=;Password=";

    private readonly Func<DataContext> getDb;

    public Tests()
    {
        var dbBuilder = new DbContextOptionsBuilder<DataContext>();
        dbBuilder.UseNpgsql(Connection);
        dbBuilder.Options.Freeze();

        getDb = () => new DataContext(dbBuilder.Options);
    }

    [Fact]
    public async Task ComplexObject_OnlyProperty_HasValue_Works()
    {
        var sourceDate = new DateOnly(2026, 02, 24);
        var copyToDate = sourceDate.AddDays(3);
        var parent = new ParentModel("something");
        parent.Update(sourceDate, 10);
        parent.Copy(sourceDate, copyToDate);

        using var saveDb = getDb();
        saveDb.Add(parent);
        await saveDb.SaveChangesAsync();

        using var loadDb = getDb();
        var loadedParent = await loadDb.GetChildForDate(parent.Id, copyToDate);

        Assert.Single(loadedParent.Children);
        Assert.NotNull(loadedParent.Children[0].Complex);
    }

    [Fact]
    public async Task ComplexObject_OnlyProperty_HasNullValue_WorksIn9_FailsIn10()
    {
        var sourceDate = new DateOnly(2026, 02, 24);
        var copyToDate = sourceDate.AddDays(3);
        var parent = new ParentModel("something");
        parent.Update(sourceDate, 10);
        parent.Copy(sourceDate, copyToDate);

        using var saveDb = getDb();
        saveDb.Add(parent);
        await saveDb.SaveChangesAsync();

        using var loadDb = getDb();
        var loadedParent = await loadDb.GetChildForDate(parent.Id, sourceDate);

        Assert.Single(loadedParent.Children);
        Assert.NotNull(loadedParent.Children[0].Complex);
    }

    public async Task DisposeAsync()
    {
        using var db = getDb();
        await db.Database.EnsureDeletedAsync();
    }

    public async Task InitializeAsync()
    {
        using var db = getDb();
        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();
    }
}
