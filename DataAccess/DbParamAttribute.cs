namespace MidnightApi.DataAccess;

[AttributeUsage(AttributeTargets.Property)]
public sealed class DbParamAttribute : Attribute
{
    public string Name { get; }

    public DbParamAttribute(string name)
    {
        Name = name;
    }
}
