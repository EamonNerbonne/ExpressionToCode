using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Collections.Generic;

namespace ExpressionToCodeLib {
	static class ReflectionHelpers {
		public static PropertyInfo GetPropertyIfGetter(MethodInfo mi) {
			bool supposedGetter = mi.Name.StartsWith("get_");
			//bool supposedSetter = mi.Name.StartsWith("set_");

			if (!mi.IsSpecialName || !supposedGetter) return null;
			var pName = mi.Name.Substring(4);
			const BindingFlags bindingFlags =
				BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
			var pars = mi.GetParameters();
			if (pars.Length == 0)
				return mi.DeclaringType.GetProperty(pName, bindingFlags);
			else
				return mi.DeclaringType.GetProperty(pName, bindingFlags, null, mi.ReturnType,
					pars.Select(p => p.ParameterType).ToArray(), null);
		}

		public static bool IsMemberInfoStatic(MemberInfo mi) {
			if (mi is FieldInfo)
				return ((FieldInfo) mi).IsStatic;
			else if (mi is MethodInfo)
				return (((MethodInfo) mi).Attributes & MethodAttributes.Static) == MethodAttributes.Static;
			else if (mi is PropertyInfo) {
				PropertyInfo pi = (PropertyInfo) mi;
				return pi.CanRead ? pi.GetGetMethod().IsStatic : pi.GetSetMethod().IsStatic;
			} else if (mi.MemberType == MemberTypes.NestedType)
				return true;
			else if (mi is EventInfo)
				return ((EventInfo) mi).GetAddMethod(true).IsStatic;
			else
				throw new ArgumentOutOfRangeException("mi",
					"Expression represents a member access for member" + mi.Name + " of membertype " + mi.MemberType
						+ " that is unsupported");
		}

		public static bool HasBuiltinConversion(Type from, Type to) {
			return
				from == typeof (sbyte)
					&& (to == typeof (short) || to == typeof (int) || to == typeof (long) || to == typeof (float)
						|| to == typeof (double) || to == typeof (decimal))
					|| from == typeof (byte)
						&& (to == typeof (short) || to == typeof (ushort) || to == typeof (int) || to == typeof (uint)
							|| to == typeof (long) || to == typeof (ulong) || to == typeof (float) || to == typeof (double)
							|| to == typeof (decimal))
					|| from == typeof (short)
						&& (to == typeof (int) || to == typeof (long) || to == typeof (float) || to == typeof (double)
							|| to == typeof (decimal))
					|| from == typeof (ushort)
						&& (to == typeof (int) || to == typeof (uint) || to == typeof (long) || to == typeof (ulong)
							|| to == typeof (float) || to == typeof (double) || to == typeof (decimal))
					|| from == typeof (int)
						&& (to == typeof (long) || to == typeof (float) || to == typeof (double) || to == typeof (decimal))
					|| from == typeof (uint)
						&& (to == typeof (long) || to == typeof (ulong) || to == typeof (float) || to == typeof (double)
							|| to == typeof (decimal))
					|| from == typeof (long) && (to == typeof (float) || to == typeof (double) || to == typeof (decimal))
					|| from == typeof (char)
						&& (to == typeof (ushort) || to == typeof (int) || to == typeof (uint) || to == typeof (long)
							|| to == typeof (ulong) || to == typeof (float) || to == typeof (double) || to == typeof (decimal))
					|| from == typeof (float) && (to == typeof (double))
					|| from == typeof (ulong) && (to == typeof (float) || to == typeof (double) || to == typeof (decimal))
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

		public enum TypeClass {
			BuiltinType,
			AnonymousType,
			ClosureType,
			StructType,
			NormalType,
		}

		public static TypeClass GuessTypeClass(this Type type) {
			bool compilerGenerated = type.GetCustomAttributes(typeof (CompilerGeneratedAttribute), false).Any();
			string name = type.Name;
			bool name_StartWithLessThan = name.StartsWith("<");
			bool isBuiltin = type.IsPrimitive || type.IsEnum || type == typeof (decimal) || type == typeof (string)
				|| typeof (Type).IsAssignableFrom(type);

			if (name_StartWithLessThan && compilerGenerated) {
				bool named_AnonymousType = name.Contains("AnonymousType");
				bool named_DisplayClass = name.Contains("DisplayClass");
				bool isGeneric = type.IsGenericType;
				bool isNested = type.IsNested;

				if (!isBuiltin && isGeneric && !isNested && named_AnonymousType)
					return TypeClass.AnonymousType;
				else if (!isBuiltin && !isGeneric && isNested && named_DisplayClass)
					return TypeClass.ClosureType;
						//note that since genericness+nestedness don't overlap, these typeclasses aren't confusable.
				else
					throw new ArgumentException("Can't deal with unknown-style compiler generated class " + type.FullName);
			} else if (!compilerGenerated && !name_StartWithLessThan)
				return isBuiltin ? TypeClass.BuiltinType : type.IsValueType ? TypeClass.StructType : TypeClass.NormalType;
			else
				throw new ArgumentException("Unusual type, heuristics uncertain:" + name);
		}
	}
}