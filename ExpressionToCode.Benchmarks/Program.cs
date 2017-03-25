using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using ExpressionToCodeLib;
using JetBrains.Annotations;

namespace ExpressionToCode.Benchmarks
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BenchmarkRunner.Run<BenchmarkPAssert>();
        }
    }

    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class BenchmarkPAssert
    {
        static readonly ExpressionToCodeConfiguration
            baseLineConfiguration = ExpressionToCodeConfiguration.DefaultCodeGenConfiguration,
            withOptimizationConfiguration = ExpressionToCodeConfiguration.DefaultCodeGenConfiguration.WithCompiler(ExpressionTreeCompilers.OptimizedExpressionCompiler);

        readonly Expression<Func<bool>> testExpr = GetExpression();

        static Expression<Func<bool>> GetExpression()
        {
            var x = 1;
            var s = "Test";
            return () => x == 1 && (s.Contains("S") || s.Contains("s"));
        }

        static Func<bool> GetFunc()
        {
            var x = 1;
            var s = "Test";
            return () => x == 1 && (s.Contains("S") || s.Contains("s"));
        }

        static void BaseLineAssert(Func<bool> assertion)
        {
            if (!assertion()) {
                throw new Exception();
            }
        }

        [Benchmark]
        public void JustCompile()
        {
            testExpr.Compile();
        }

        [Benchmark]
        public void JustFastCompile()
        {
            ExpressionTreeCompilers.OptimizedExpressionCompiler.Compile(testExpr);
        }

        [Benchmark]
        public void PAssertWithCompile()
        {
            baseLineConfiguration.Assert(GetExpression());
        }

        [Benchmark]
        public void PAssertWithFastCompile()
        {
            withOptimizationConfiguration.Assert(GetExpression());
        }

        [Benchmark]
        public void BaseLinePlainLambdaExec()
        {
            BaseLineAssert(GetFunc());
        }
    }
}
