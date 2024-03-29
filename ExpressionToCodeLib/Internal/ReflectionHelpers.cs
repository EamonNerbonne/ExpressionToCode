namespace ExpressionToCodeLib.Internal;

static class ReflectionHelpers
{
    public static PropertyInfo? GetPropertyIfGetter(MethodInfo mi)
    {
        var supposedGetter = mi.Name.StartsWith("get_", StringComparison.Ordinal);

        if (!mi.IsSpecialName || !supposedGetter) {
            return null;
        }

        var pName = mi.Name.Substring(4);
        const BindingFlags bindingFlags =
            BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
        var pars = mi.GetParameters();
        var declaringType = mi.DeclaringType;
        if (declaringType is null) {
            return null;
        } else if (pars.Length == 0) {
            return declaringType.GetTypeInfo().GetProperty(pName, bindingFlags);
        } else {
            // ReSharper disable once PossibleNullReferenceException
            foreach (var prop in declaringType.GetProperties(bindingFlags)) {
                if (prop.GetMethod == mi) {
                    return prop;
                }
            }

            return null;
        }
    }

    public static bool IsMemberInfoStatic(MemberInfo mi)
        => mi switch {
            FieldInfo fieldInfo => fieldInfo.IsStatic,
            MethodInfo methodInfo => (methodInfo.Attributes & MethodAttributes.Static) == MethodAttributes.Static,
            PropertyInfo pi => (pi.GetGetMethod(true) ?? pi.GetSetMethod(true))?.IsStatic ?? throw new("a property must have either a getter or setter"),
            EventInfo eventInfo => eventInfo.GetAddMethod(true)?.IsStatic ?? throw new("An event must have a backing add method"),
            { MemberType: MemberTypes.NestedType, } => true,
            _ => throw new ArgumentOutOfRangeException(nameof(mi), "Expression represents a member access for member" + mi.Name + " of member-type " + mi.MemberType + " that is unsupported"),
        };

    public static bool HasBuiltinConversion(Type from, Type to)
        => from == typeof(sbyte)
            && (to == typeof(short) || to == typeof(int) || to == typeof(long) || to == typeof(float)
                || to == typeof(double) || to == typeof(decimal))
            || from == typeof(byte)
            && (to == typeof(short) || to == typeof(ushort) || to == typeof(int) || to == typeof(uint)
                || to == typeof(long) || to == typeof(ulong) || to == typeof(float) || to == typeof(double)
                || to == typeof(decimal))
            || from == typeof(short)
            && (to == typeof(int) || to == typeof(long) || to == typeof(float) || to == typeof(double)
                || to == typeof(decimal))
            || from == typeof(ushort)
            && (to == typeof(int) || to == typeof(uint) || to == typeof(long) || to == typeof(ulong)
                || to == typeof(float) || to == typeof(double) || to == typeof(decimal))
            || from == typeof(int)
            && (to == typeof(long) || to == typeof(float) || to == typeof(double) || to == typeof(decimal))
            || from == typeof(uint)
            && (to == typeof(long) || to == typeof(ulong) || to == typeof(float) || to == typeof(double)
                || to == typeof(decimal))
            || from == typeof(long) && (to == typeof(float) || to == typeof(double) || to == typeof(decimal))
            || from == typeof(char)
            && (to == typeof(ushort) || to == typeof(int) || to == typeof(uint) || to == typeof(long)
                || to == typeof(ulong) || to == typeof(float) || to == typeof(double) || to == typeof(decimal))
            || from == typeof(float) && to == typeof(double)
            || from == typeof(ulong) && (to == typeof(float) || to == typeof(double) || to == typeof(decimal));

    public static bool CanImplicitlyCast(Type from, Type to)
        => to.GetTypeInfo().IsAssignableFrom(from) || HasBuiltinConversion(from, to);

    public enum TypeClass
    {
        BuiltinType,
        AnonymousType,
        ClosureType,
        StructType,
        NormalType,
        TopLevelProgramClosureType,
    }

    public static TypeClass GuessTypeClass(this Type type)
    {
        var typeInfo = type.GetTypeInfo();
        if (typeInfo.IsArray) {
            return TypeClass.BuiltinType;
        }

        var compilerGenerated = typeInfo.GetCustomAttributes(typeof(CompilerGeneratedAttribute), false).Any();
        var name = type.Name;
        var named_DisplayClass = name.Contains("_DisplayClass");
        var name_StartWithLessThan = name.StartsWith("<", StringComparison.Ordinal);
        var isBuiltin = typeInfo.IsPrimitive || typeInfo.IsEnum || type == typeof(decimal) || type == typeof(string)
            || typeof(Type).GetTypeInfo().IsAssignableFrom(type);

        if (name_StartWithLessThan && compilerGenerated) {
            var named_AnonymousType = name.Contains("AnonymousType");
            var isGeneric = typeInfo.IsGenericType;
            var isNested = type.IsNested;

            if (!isBuiltin && isGeneric && !isNested && named_AnonymousType) {
                return TypeClass.AnonymousType;
            } else if (!isBuiltin && isNested && named_DisplayClass) {
                return TypeClass.ClosureType;
            }
            //note that since genericness+nestedness don't overlap, these typeclasses aren't confusable.
            else {
                throw new ArgumentException(
                    "Can't deal with unknown-style compiler generated class " + type.FullName + " " + named_AnonymousType + ", " + named_DisplayClass + ", " + isGeneric
                    + ", " + isNested);
            }
        } else if (!compilerGenerated && !name_StartWithLessThan) {
            return isBuiltin ? TypeClass.BuiltinType : typeInfo.IsValueType ? TypeClass.StructType : TypeClass.NormalType;
        } else if (!compilerGenerated && name_StartWithLessThan && named_DisplayClass && type.Namespace is null && type.IsNested && type.DeclaringType == type.Assembly.EntryPoint?.DeclaringType) {
            return TypeClass.TopLevelProgramClosureType;
        } else {
            throw new ArgumentException("Unusual type, heuristics uncertain:" + name);
        }
    }
}
