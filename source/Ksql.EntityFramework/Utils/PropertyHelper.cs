using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Ksql.EntityFramework.Utils
{
    public static class PropertyHelper
    {
        public static IEnumerable<PropertyInfo> GetKeyProperties(Type type)
        {
            return type.GetProperties()
                       .Where(p => Attribute.IsDefined(p, typeof(KeyAttribute)));
        }
    }
}
