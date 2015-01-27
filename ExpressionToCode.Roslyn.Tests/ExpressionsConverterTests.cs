/*
 * Created by SharpDevelop.
 * User: dzmitry_lahoda
 * Date: 2015-01-20
 * Time: 18:22:01
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using ExpressionToCode.Roslyn;

namespace ExpressionToCode.Roslyn.Tests
{
    [TestFixture]
    public class ExpressionsConverterTests
    {
        
        [Test]
        public void NewObject(){
            
            var syntax = CSharpSyntaxTree.ParseText(@"() => new object()");
            
            var actual = ExpressionsConverter.ToSyntaxTree(() => new Object());

            Assert.AreEqual(syntax.Length, actual.Length );
        }
        
        [Test]
        [Ignore("TODO: Wrap delegate into static class with 'public static Func<object> Create(){return () => new object();}'")]
        [Description("Shows that we can automatically go from compilation of delegates out of expressions into full blown emitting")]
        public void NewObjectFactory(){
            
            var actual = ExpressionsConverter.ToCompilationUnit(() => new Object());
            var stream = new MemoryStream();
            var result = actual.Emit(stream);
            Assert.AreEqual(0,result.Diagnostics.Count());
            
        }
    }
}