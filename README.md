# efcore-bug

Consider the following models, configured in EF Core:

- Parent with many Children
- Child has a required Complex Property
- Complex property contains methods and a single nullable DateOnly property

Like so:
```
modelBuilder.Entity<ParentModel>()
    .HasMany(p => p.Children);

modelBuilder.Entity<ChildModel>()
    .ComplexProperty(c => c.Complex)
    .IsRequired();
```

If all of the following are true:

- The parent is the target of the dbContext query
- A child is included with a where predicate
- The child's Complex property has null for its DateOnly property

Like so:
```
await db.Parents
.Include(p => p.Children.Where(c => c.Date == asOfDate))
.SingleAsync(p => p.Id == id);
```

**Problem:** In EF 10, this child will load with the complex property as null. This causes runtime null reference exceptions.
**Expected:** In EF 9, this child will load with the complex property as an object, with its DateOnly property null.

## To test:

Go to `Tests.cs` and change your connection string to one appropriate for your setup. (I used npgsql because I have postgres installed, but you may want to modify the code to support Sql Server instead.)

Run `dotnet test .\EfCoreBug.csproj` to see the failing test `ComplexObject_OnlyProperty_HasNullValue_WorksIn9_FailsIn10`.

Run `dotnet test .\EfCoreBug.csproj -c EF9` to see the test pass.
