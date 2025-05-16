namespace Ksql.EntityFramework.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class DateTimeFormatAttribute : Attribute
{
    public string? Format { get; set; }

    public string? Locale { get; set; }
}
