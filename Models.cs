namespace EfCoreBug;

/// <summary>
/// An object that has different data on different dates
/// </summary>
public class ParentModel
{
    public int Id { get; private set; }
    public List<ChildModel> Children { get; private set; }
    public string Name { get; private set; }
    protected ParentModel()
    {
        Children = [];
    }

    public ParentModel(string name)
    {
        Name = name;
        Children = [];
    }

    public void Update(DateOnly date, int value)
    {
        var child = Children.SingleOrDefault(d => d.Date == date);

        if (child != null)
            child.Update(value);
        else
            Children.Add(new(date, value));
    }

    public bool Copy(DateOnly copyFrom, DateOnly copyTo)
    {
        var from = Children.Single(d => d.Date == copyFrom);
        var to = Children.SingleOrDefault(d => d.Date == copyTo);

        if (to != null)
            return to.CopyIfNewer(from);

        Children.Add(ChildModel.Copy(from, copyTo));
        return true;
    }
}

/// <summary>
/// An object that holds the value for a specific date, whether that data is known or copied forward from the most recent data
/// </summary>
public class ChildModel
{
    public int Id { get; private set; }
    public DateOnly Date { get; private set; }
    public int Value { get; private set; }
    public ComplexModel Complex { get; private set; }

    public ChildModel(DateOnly date, int value) 
    {
        Complex = new();
        Date = date;
        Value = value;
    }

    public void Update(int value)
    {
        Value = value;
        Complex.MarkOriginal();
    }

    public bool CopyIfNewer(ChildModel childModel)
    {
        if (!Complex.ShouldCopy(childModel.Date))
            return false;

        Value = childModel.Value;
        Complex.MarkCopied(childModel.Date);
        return true;
    }

    public static ChildModel Copy(ChildModel childModel, DateOnly date)
    {
        var self = new ChildModel(date, childModel.Value);
        self.Complex.MarkCopied(childModel.Date);
        return self;
    }
}

/// <summary>
/// An object that can be attached to many models and assists with figuring out whether the model is copied or original
/// </summary>
public class ComplexModel
{
    public DateOnly? CopiedDate { get; private set; }

    public bool IsCopied() => CopiedDate != null;

    public bool ShouldCopy(DateOnly date) => IsCopied() && date >= CopiedDate; 

    public void MarkCopied(DateOnly date) => CopiedDate = date;

    public void MarkOriginal() => CopiedDate = null;
}