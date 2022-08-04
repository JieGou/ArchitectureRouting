using System ;
using System.Reflection ;

namespace Arent3d.Architecture.Routing.Storages.Extensions
{
    public static class AttributeExtension
    {
        public static TAttribute GetAttribute<TAttribute>(this MemberInfo memberInfo) where TAttribute : Attribute
        {
            var attributes = memberInfo.GetCustomAttributes(typeof(TAttribute), false);
            if (attributes.Length == 0 || attributes[0] is not TAttribute attribute)
                throw new InvalidOperationException($"The property {memberInfo.Name} does not have a {typeof(TAttribute)}.");

            return attribute;
        }
    }
}