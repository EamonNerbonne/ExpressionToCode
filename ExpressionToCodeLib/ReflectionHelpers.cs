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
				return ((MethodInfo)mi).Attributes.HasFlag(MethodAttributes.Static);
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
	}
}
