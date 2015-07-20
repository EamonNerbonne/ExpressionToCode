using System;
using ExpressionToCodeLib.Unstable_v2_Api;
using Xunit;
using System.Linq;
using System.Linq.Expressions;

namespace ExpressionToCodeTest
{
	public class ExpressionWithNameTest
	{
		[Fact]
		public void TheVariable_ToNameOf()
		{
			var theVariable = "theValue";
			var actual = ExpressionWithName.ToNameOf(() => theVariable);
			Assert.Equal("theVariable",actual);
		}



		[Fact]
		public void TheMethod_ToNameOf()
		{
			Expression<Func<int, string, string>> theMethod = (x, y) => TheMethod(x, y);
			var actual = ExpressionWithName.ToNameOf(theMethod);
			Assert.Equal("TheMethod",actual);
		}

		[Fact]
		public void TheMethod_ToFullNameOf()
		{
			Expression<Func<int, string, string>> theMethod = (x, y) => ExpressionWithNameTest.TheMethod(x, y);
			var actual = ExpressionWithName.ToFullNameOf(theMethod);
			Assert.Equal("ExpressionWithNameTest.TheMethod(x, y)",actual);
		}


		[Fact]
		public void TheGenericMethod_ToNameOf()
		{
			Expression<Func<string>> theGenericMethod = () => TheGenericMethod<int>(2);
			var actual = ExpressionWithName.ToNameOf(theGenericMethod);
			Assert.Equal("TheGenericMethod",actual);
		}



		[Fact]
		public void TheSimpleMethod_ToNameOf()
		{
			Expression<Action> theSimpleMethod = () => TheSimpleMethod(); 
			var actual = theSimpleMethod.ToNameOf();
			Assert.Equal("TheSimpleMethod",actual);
		}

		public void TheSimpleMethod()
		{
		}
		static string TheProperty { get { return "TheValue"; } }
		// ReSharper disable once UnusedParameter.Local
		string this[int index] { get { return "TheIndexedValue"; } }
		static int StaticReturnZero() { return 0; }
		// ReSharper disable MemberCanBeMadeStatic.Local
		static string TheMethod(int parameter1, string parameter2) { return "TheMethod " + parameter1 + " " + parameter2; }
		// ReSharper disable once UnusedTypeParameter
		static string TheGenericMethod<T>(int two) { return "Return value is " + two * two; }
		int ReturnZero() { return 0; }
	}
}

