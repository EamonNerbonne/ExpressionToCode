using System;
using System.Linq.Expressions;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace ExpressionToCode.Roslyn
{
    /// <summary>
    /// Converts <see cref="System.Linq.Expressions.Expression">expression</see> to <see cref="SyntaxTree"/>
    /// </summary>
    public static class ExpressionsConverter
    {
        public static SyntaxTree ToSyntaxTree<T>(Expression<Func<T>> e) { return ToSyntaxTree((Expression)e); }

        public static SyntaxTree ToSyntaxTree(Expression e)
        {
            //TODO: slow but working hack, fix it my modifying ExpressionToCode visitor to allow emit Roslyn SyntaxTree
            return CSharpSyntaxTree.ParseText(ExpressionToCodeLib.ExpressionToCode.ToCode(e));
        }

        public static CSharpCompilation ToCompilationUnit<T>(Expression<Func<T>> e) { return ToCompilationUnit((Expression)e); }

        public static CSharpCompilation ToCompilationUnit(Expression e)
        {
            //TODO: dump with all namespaces to prevent name clash
            var tree = CSharpSyntaxTree.ParseText(ExpressionToCodeLib.ExpressionToCode.ToCode(e));

            //TODO: grab all references from Expression tree
            var ref1 = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
            var ref2 = MetadataReference.CreateFromFile(typeof(Func<>).Assembly.Location);

            var compilation = CSharpCompilation.Create("ExpressionsConverter.ToCompilationUnit", new List<SyntaxTree> { tree }, new List<MetadataReference> { ref1, ref2 });
            return compilation;
        }
    }
}
