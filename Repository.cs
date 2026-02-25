using Microsoft.EntityFrameworkCore;

namespace EfCoreBug;

public static class Repository
{
    public static async Task<ParentModel> GetChildForDate(this DataContext db, int id, DateOnly asOfDate) =>
        await db.Parents
        .Include(p => p.Children.Where(c => c.Date == asOfDate))
        .SingleAsync(p => p.Id == id);

    public static async Task<ParentModel> GetChildForDateThenInclude(this DataContext db, int id, DateOnly asOfDate) =>
        await db.Parents
        .Include(p => p.Children.Where(c => c.Date == asOfDate))
        .ThenInclude(p => p.Complex)
        .SingleAsync(p => p.Id == id);

    public static async Task<ParentModel> GetChildren(this DataContext db, int id) =>
        await db.Parents
        .Include(p => p.Children)
        .SingleAsync();

    public static async Task<ChildModel> LoadDirectly(this DataContext db, int id) =>
        await db.Children.SingleAsync(c => c.Id == id);
}
