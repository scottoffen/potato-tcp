using System;
using System.Collections.Generic;

namespace PotatoTcp.HandlerStrategies
{
    public static class TypeExtensions
    {
        public static IEnumerable<Type> GetBaseTypes(this Type handlerType)
        {
            var objType = typeof(object);
            var baseType = handlerType.BaseType;
            while (baseType != null && baseType != objType)
            {
                yield return baseType;
                baseType = baseType.BaseType;
            }
        }
    }
}