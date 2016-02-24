using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using ExpressionToCodeLib;

namespace ExpressionToCode.Benchmarks
{
    public class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<BenchmarkCompile>();
            //BenchmarkRunner.Run<BenchmarkPAssert>();
            Console.ReadKey();
        }
    }

    public class BenchmarkCompile
    {
        readonly Expression<Func<bool>> testExpr = GetExpression();

        static Expression<Func<bool>> GetExpression()
        {
            var x = 1;
            return () => x == 1;
        }

        [Benchmark]
        public void Compile() { testExpr.Compile(); }

        [Benchmark]
        public void Emit() { OptimizedExpressionCompiler.Compile(testExpr); }
    }

    public class BenchmarkPAssert
    {
        [Benchmark]
        public void PAssertWithCompile()
        {
            var x = 1;
            PAssert.That(() => x == 1, emit: false);
        }

        [Benchmark]
        public void PAssertWithEmit()
        {
            var x = 1;
            PAssert.That(() => x == 1, emit: true);
        }
    }
}
