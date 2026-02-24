using Microsoft.EntityFrameworkCore;

namespace EfCoreBug;

public static class Repository
{
    public static async Task<ParentModel> GetChildForDate(this DataContext db, int id, DateOnly asOfDate) =>
        await db.Parents
        .Include(p => p.Children.Where(c => c.Date == asOfDate))
        .SingleAsync(p => p.Id == id);
}
