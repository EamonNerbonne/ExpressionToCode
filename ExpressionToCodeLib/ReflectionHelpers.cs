using System;
using System.Reflection;

namespace ExpressionToCodeLib {
	static class ReflectionHelpers {
		public static PropertyInfo GetPropertyIfGetter(MethodInfo mi) {
			bool supposedGetter = mi.Name.StartsWith("get_");
			//bool supposedSetter = mi.Name.StartsWith("set_");

			if (!mi.IsSpecialName || !supposedGetter) return null;

			PropertyInfo pi = mi.DeclaringType.GetProperty(mi.Name.Substring(4), BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
			return pi.CanRead ? pi : null;//TODO:verify.
		}
		public static bool IsMemberInfoStatic(MemberInfo mi) {
			if (mi is FieldInfo)
				return ((FieldInfo)mi).IsStatic;
			else if (mi is MethodInfo)
				return (((MethodInfo)mi).Attributes & MethodAttributes.Static) == MethodAttributes.Static;
			else if (mi is PropertyInfo) {
				PropertyInfo pi = (PropertyInfo)mi;
				return pi.CanRead ? pi.GetGetMethod().IsStatic : pi.GetSetMethod().IsStatic;
			} else if (mi.MemberType == MemberTypes.NestedType)
				return true;
			else if (mi is EventInfo)
				return ((EventInfo)mi).GetAddMethod(true).IsStatic;
			else
				throw new ArgumentOutOfRangeException("e", "Expression represents a member access for member" + mi.Name + " of membertype " + mi.MemberType + " that is unsupported");
		}

		public static bool HasBuiltinConversion(Type from, Type to) {
			return false
			|| from == typeof(sbyte) && (to == typeof(short) || to == typeof(int) || to == typeof(long) || to == typeof(float) || to == typeof(double) || to == typeof(decimal))
			|| from == typeof(byte) && (to == typeof(short) || to == typeof(ushort) || to == typeof(int) || to == typeof(uint) || to == typeof(long) || to == typeof(ulong) || to == typeof(float) || to == typeof(double) || to == typeof(decimal))
			|| from == typeof(short) && (to == typeof(int) || to == typeof(long) || to == typeof(float) || to == typeof(double) || to == typeof(decimal))
			|| from == typeof(ushort) && (to == typeof(int) || to == typeof(uint) || to == typeof(long) || to == typeof(ulong) || to == typeof(float) || to == typeof(double) || to == typeof(decimal))
			|| from == typeof(int) && (to == typeof(long) || to == typeof(float) || to == typeof(double) || to == typeof(decimal))
			|| from == typeof(uint) && (to == typeof(long) || to == typeof(ulong) || to == typeof(float) || to == typeof(double) || to == typeof(decimal))
			|| from == typeof(long) && (to == typeof(float) || to == typeof(double) || to == typeof(decimal))
			|| from == typeof(char) && (to == typeof(ushort) || to == typeof(int) || to == typeof(uint) || to == typeof(long) || to == typeof(ulong) || to == typeof(float) || to == typeof(double) || to == typeof(decimal))
			|| from == typeof(float) && (to == typeof(double))
			|| from == typeof(ulong) && (to == typeof(float) || to == typeof(double) || to == typeof(decimal))
			;
		}

		public static bool CanImplicitlyCast(Type from, Type to) {
			return to.IsAssignableFrom(from) || HasBuiltinConversion(from, to);

			//TODO: extend with op_Implicit support.
			//Use this to test if a conversion is required.
			//That means expressions checking if a child is a conversion, 
			//and if so checking if the conversion can be implicit,
			//and if so checking if the code compiles without conversion
			//if it does NOT compile, and IS implicit, then we can omit it since the compiler will add it.
		}
	}
}
