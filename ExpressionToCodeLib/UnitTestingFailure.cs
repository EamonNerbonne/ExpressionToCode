using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace ExpressionToCodeLib
{
    static class UnitTestingFailure
    {
        static Func<T0, TR> F<T0, TR>(Func<T0, TR> f) { return f; }
        static Func<T0, T1, TR> F<T0, T1, TR>(Func<T0, T1, TR> f) { return f; }
        static UnitTestingFailure() { }
        public static readonly Func<string, Exception, Exception> AssertionExceptionFactory = GetAssertionExceptionFactory();

        static Func<string, Exception, Exception> GetAssertionExceptionFactory()
        {
            return GetExceptionFactories().Where(t => t.Item2 != null).OrderByDescending(t => t.Item1).First().Item2;
        }

        static IEnumerable<Tuple<int, Func<string, Exception, Exception>>> GetExceptionFactories()
        {
            var failureMessageArg = Expression.Parameter(typeof(string), "failureMessage");
            var innerExceptionArg = Expression.Parameter(typeof(Exception), "innerException");
            var mkFailFunc = F(
                (Assembly assembly, string typename) => {
                    var exType = assembly.GetType(typename);
                    if (exType == null) {
                        return null;
                    }
                    var exConstructor = exType.GetConstructor(new[] { typeof(string), typeof(Exception) });
                    if (exConstructor == null) {
                        return null;
                    }
                    return
                        Expression.Lambda<Func<string, Exception, Exception>>(
                            Expression.New(exConstructor, failureMessageArg, innerExceptionArg),
                            failureMessageArg,
                            innerExceptionArg)
                            .Compile();
                });

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly assembly in assemblies) {
                string assemblyName = assembly.GetName().Name;
                if (assemblyName == "xunit" || assemblyName == "xunit.assert") {
                    var xUnitExceptionType = assembly.GetType("Xunit.Sdk.XunitException") ?? assembly.GetType("Xunit.Sdk.AssertException");
                    var xUnitExceptionConstructor =
                        xUnitExceptionType.GetConstructor(
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                            null,
                            new[] { typeof(string), typeof(Exception) },
                            null);

                    var assemblyBuilder =
                        AppDomain.CurrentDomain.DefineDynamicAssembly(
                            new AssemblyName("Dynamic xUnit ExpressionToCode integration"),
                            AssemblyBuilderAccess.Run);

                    var moduleBuilder = assemblyBuilder.DefineDynamicModule("xUnit_ExpressionToCode_integration");

                    var typeBuilder = moduleBuilder.DefineType("PAssertException", TypeAttributes.Public, xUnitExceptionType);

                    var constructorBuilder = typeBuilder.DefineConstructor(
                        MethodAttributes.Public,
                        CallingConventions.Standard,
                        new[] { typeof(string), typeof(Exception) });
                    var ilgen = constructorBuilder.GetILGenerator();
                    ilgen.Emit(OpCodes.Ldarg_0);
                    ilgen.Emit(OpCodes.Ldarg_1);
                    ilgen.Emit(OpCodes.Ldarg_2);
                    ilgen.Emit(OpCodes.Call, xUnitExceptionConstructor);
                    ilgen.Emit(OpCodes.Ret);

                    var exType = typeBuilder.CreateType();
                    var exConstructor = exType.GetConstructor(new[] { typeof(string), typeof(Exception) });

                    yield return
                        Tuple.Create(
                            3,
                            Expression.Lambda<Func<string, Exception, Exception>>(
                                Expression.New(
                                    exConstructor,
                                    Expression.Add(Expression.Constant("\r\n"), failureMessageArg, ((Func<string, string, string>)string.Concat).Method),
                                    innerExceptionArg),
                                failureMessageArg,
                                innerExceptionArg)
                                .Compile());
                } else if (assemblyName == "nunit.framework") {
                    yield return Tuple.Create(2, mkFailFunc(assembly, "NUnit.Framework.AssertionException"));
                } else if (assemblyName == "Microsoft.VisualStudio.QualityTools.UnitTestFramework") {
                    yield return
                        Tuple.Create(1, mkFailFunc(assembly, "Microsoft.VisualStudio.TestTools.UnitTesting.AssertFailedException"));
                }
            }
            yield return Tuple.Create(0, F((string s, Exception e) => (Exception)new AssertFailedException(s, e)));
        }
    }
}
