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

    private record SeedInfo(ParentModel Parent, DateOnly SourceDate, DateOnly CopiedDate);

    private async Task<SeedInfo> Seed()
    {
        var sourceDate = new DateOnly(2026, 02, 24);
        var copyToDate = sourceDate.AddDays(3);
        var parent = new ParentModel("something");
        parent.Update(sourceDate, 10);
        parent.Copy(sourceDate, copyToDate);

        using var saveDb = getDb();
        saveDb.Add(parent);
        await saveDb.SaveChangesAsync();

        return new(parent, sourceDate, copyToDate);
    }

    [Fact]
    // If the complex property only contains a null date, the complex property is loaded from EF 9 as an object with a null date property.
    // But in EF 10, it is loaded as a null object. This leads to null reference exceptions that weren't there before.
    public async Task ComplexObject_OnlyProperty_HasNullValue_WorksIn9_FailsIn10_FilteredInclude()
    {
        var seedInfo = await Seed();

        using var loadDb = getDb();
        // load the original object with a null for the date in the complex property
        var loadedParent = await loadDb.GetChildForDate(seedInfo.Parent.Id, seedInfo.SourceDate);

        Assert.Single(loadedParent.Children);
        Assert.NotNull(loadedParent.Children[0].Complex); // Fails in EF 10, Passes in EF 9
    }

    [Fact]
    // If the complex property only contains a null date, the complex property is loaded from EF 9 as an object with a null date property.
    // But in EF 10, it is loaded as a null object. This leads to null reference exceptions that weren't there before.
    public async Task ComplexObject_OnlyProperty_HasNullValue_WorksIn9_FailsIn10_NoFilter()
    {
        var seedInfo = await Seed();

        using var loadDb = getDb();
        // load the original object with a null for the date in the complex property
        var loadedParent = await loadDb.GetChildren(seedInfo.Parent.Id);

        Assert.All(loadedParent.Children, c => Assert.NotNull(c.Complex)); // Fails in EF 10, Passes in EF 9
    }

    #region EXTRA OBSERVATIONS

    [Fact]
    // If the complex property contains a non-null date, it is loaded as an object with that property correctly in EF 9 and EF 10
    public async Task ComplexObject_OnlyProperty_HasValue_Works()
    {
        var seedInfo = await Seed();

        using var loadDb = getDb();
        // load the copied object with a value for the date in the complex property
        var loadedParent = await loadDb.GetChildForDate(seedInfo.Parent.Id, seedInfo.CopiedDate);

        Assert.Single(loadedParent.Children);
        Assert.NotNull(loadedParent.Children[0].Complex);
    }

    [Fact]
    // If the child object is loaded directly, the complex property is not null in either EF version
    public async Task ComplexObject_OnlyProperty_HasNullValue_WorksIfDirectlyQueried()
    {
        var seedInfo = await Seed();

        using var loadDb = getDb();
        // load the original object with a null for the date in the complex property, bypassing the parent
        var loadedChild = await loadDb.LoadDirectly(seedInfo.Parent.Children.First(d => d.Date == seedInfo.SourceDate).Id);

        Assert.NotNull(loadedChild.Complex); // Fails in EF 10, Passes in EF 9
    }

    [Fact]
    // Includes cannot be used to help with this bug
    public async Task ComplexObject_OnlyProperty_ThenInclude_Invalid()
    {
        var seedInfo = await Seed();

        using var loadDb = getDb();
        // load the original object with a null for the date in the complex property, attempting an includes
        Func<Task<ParentModel>> exceptionFn = () => loadDb.GetChildForDateThenInclude(seedInfo.Parent.Id, seedInfo.SourceDate);

        await Assert.ThrowsAsync<InvalidOperationException>(exceptionFn);
    }

    #endregion

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
