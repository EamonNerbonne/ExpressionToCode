/*
 * Created by SharpDevelop.
 * User: dzmitry_lahoda
 * Date: 2015-01-20
 * Time: 18:21:24
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Linq.Expressions;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Reflection;

namespace ExpressionToCode.Roslyn
{
	/// <summary>
	/// Converts <see cref="System.Linq.Expressions.Expression">expression</see> to <see cref="SyntaxTree"/>
	/// </summary>
	public static class ExpressionsConverter
	{
		public static SyntaxTree ToSyntaxTree<T>(Expression<Func<T>> e) { return ToSyntaxTree((Expression)e); }
		
		
        public static SyntaxTree ToSyntaxTree(Expression e) {
			//TODO: slow but working hack, fix it my modifying ExpressionToCode visitor to allow emit Roslyn SyntaxTree
			return CSharpSyntaxTree.ParseText(ExpressionToCodeLib.ExpressionToCode.ToCode(e));
        }
		
		
	    public static CSharpCompilation ToCompilationUnit<T>(Expression<Func<T>> e) { return ToCompilationUnit((Expression)e); }
		
	    
		public static CSharpCompilation ToCompilationUnit(Expression e) {
	    	
	    	//TODO: dump with all namespaces to prevent name clash
			var tree =  CSharpSyntaxTree.ParseText(ExpressionToCodeLib.ExpressionToCode.ToCode(e));
			
			//TODO: grap all references from Expression tree
			var ref1 = MetadataReference.CreateFromAssembly(typeof(object).Assembly);
			var ref2 = MetadataReference.CreateFromAssembly(typeof(Func<>).Assembly);
			
			var compilation = Microsoft.CodeAnalysis.CSharp.CSharpCompilation.Create("ExpressionsConverter.ToCompilationUnit",new List<SyntaxTree>{tree}, new List<MetadataReference>() {ref1,ref2});
			return compilation;
        }
	}
}