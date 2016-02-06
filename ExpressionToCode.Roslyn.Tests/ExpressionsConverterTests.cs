using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace ExpressionToCode.Roslyn.Tests {
    
    public class ExpressionsConverterTests {
        [Fact(Skip ="this currently crashes the vs test runner, disabling")]
        public void NewObject() {
            var syntax = CSharpSyntaxTree.ParseText(@"() => new object()");

            var actual = ExpressionsConverter.ToSyntaxTree(() => new Object());

            Assert.Equal(syntax.Length, actual.Length);
        }

		//Shows that we can automatically go from compilation of delegates out of expressions into full blown emitting
		[Fact(Skip = "TODO: Wrap delegate into static class with 'public static Func<object> Create(){return () => new object();}'")]
        public void NewObjectFactory() {
            var actual = ExpressionsConverter.ToCompilationUnit(() => new Object());
            var stream = new MemoryStream();
            var result = actual.Emit(stream);

            Assert.Empty(result.Diagnostics);
        }
    }
}
