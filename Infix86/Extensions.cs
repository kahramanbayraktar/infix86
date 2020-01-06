using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace AssemblyConverter
{
    public static class Extensions
    {
        public static string ToDescription<TEnum>(this TEnum value) where TEnum : struct
        {
            FieldInfo fi = value.GetType().GetField(value.ToString());

            DescriptionAttribute[] attributes = fi.GetCustomAttributes(typeof(DescriptionAttribute), false) as DescriptionAttribute[];

            if (attributes != null && attributes.Any())
            {
                return attributes.First().Description;
            }

            return value.ToString();
        }
    }
}
