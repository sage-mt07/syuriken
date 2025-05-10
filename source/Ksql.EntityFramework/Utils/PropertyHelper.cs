using System.Reflection;
using Ksql.EntityFramework.Attributes;

namespace Ksql.EntityFramework.Utils;

public static class PropertyHelper
{
    public static IEnumerable<PropertyInfo> GetKeyProperties(Type type)
    {
        return type.GetProperties()
                   .Where(p => Attribute.IsDefined(p, typeof(KeyAttribute)));
    }
}
