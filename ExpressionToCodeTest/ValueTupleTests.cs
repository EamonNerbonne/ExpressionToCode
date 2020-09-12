﻿using System;
using ExpressionToCodeLib;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using FastExpressionCompiler;
using Xunit;

namespace ExpressionToCodeTest
{
    public struct MyValueTuple<T1, T2> : IEquatable<MyValueTuple<T1, T2>>
    {
        public readonly T1 v1;
        public readonly T2 v2;

        public MyValueTuple((T1 v1, T2 v2) tuple)
            => (v1, v2) = tuple;

        public bool Equals(MyValueTuple<T1, T2> other)
            => Equals(v1, other.v1) && Equals(v2, other.v2);
    }

    public class ValueTupleTests
    {
        static MyValueTuple<T1, T2> ToMyValueTuple<T1, T2>((T1, T2) tuple)
            => new MyValueTuple<T1, T2>(tuple);

        [Fact]
        public void ExpressionWithValueTupleEqualsCanCompile()
        {
            //was a test for https://github.com/dotnet/roslyn/issues/27322
            var tupleA = (1, 3);
            var tupleB = (1, "123".Length);

            Expression<Func<int>> ok1 = () => tupleA.Item1;
            Expression<Func<int>> ok2 = () => tupleA.GetHashCode();
            Expression<Func<Tuple<int, int>>> ok3 = () => tupleA.ToTuple();
            Expression<Func<bool>> ok4 = () => Equals(tupleA, tupleB);
            Expression<Func<int>> ok5 = () => Comparer<(int, int)>.Default.Compare(tupleA, tupleB);
            ok1.Compile()();
            ok2.Compile()();
            ok3.Compile()();
            ok4.Compile()();
            ok5.Compile()();

            var myTupleA = ToMyValueTuple(tupleA);
            var myTupleB = ToMyValueTuple(tupleB);
            Expression<Func<bool>> ok6 = () => myTupleA.Equals(myTupleB);
            Expression<Func<bool>> ok7 = () => tupleA.ToTuple().Equals(tupleB.ToTuple());
            Expression<Func<bool>> ok8 = () => ToMyValueTuple(tupleA).Equals(ToMyValueTuple(tupleB));
            ok6.Compile()();
            ok7.Compile()();
            ok8.Compile()();

            // ReSharper disable once UnusedVariable
            Expression<Func<bool>> err1 = () => tupleA.Equals(tupleB); //crash
            // ReSharper disable once UnusedVariable
            Expression<Func<int>> err2 = () => tupleA.CompareTo(tupleB); //crash
        }

        [Fact]
        public void FastExpressionCompileValueTupleEqualsWorks()
        {
            //was a test for https://github.com/dotnet/roslyn/issues/27322
            var tuple = (1, 3);
            var tuple2 = (1, "123".Length);
            var expr = ExpressionCompiler.CompileFast(() => tuple.Equals(tuple2));
            Assert.True(expr());
        }

        [Fact]
        public void AssertingOnValueTupleEqualsWorks()
        {
            //was a test for https://github.com/dotnet/roslyn/issues/27322
            var tuple = (1, 3);
            var tuple2 = (1, "123".Length);
            Expression<Func<bool>> expr = () => tuple.Equals(tuple2);
            Assert.True(expr.Compile()());
            PAssert.That(() => tuple.Equals(tuple2));
        }

        [Fact]
        public void ToCSharpFriendlyTypeNameSupportsTuples()
        {
            var actual = (1, "2", new[] { 1, 2, 3 });
            Assert.Equal("(int, string, int[])", actual.GetType().ToCSharpFriendlyTypeName());
        }

        [Fact]
        public void ToCSharpFriendlyTypeNameSupportsLooongTuples()
        {
            var actual = (1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
            Assert.Equal("(int, int, int, int, int, int, int, int, int, int)", actual.GetType().ToCSharpFriendlyTypeName());
        }

        [Fact]
        public void ToCSharpFriendlyTypeNameSupportsNestedTuples()
        {
            var actual = (1, 2, ((3, 4), 5), 6, 7, 8);
            Assert.Equal("(int, int, ((int, int), int), int, int, int)", actual.GetType().ToCSharpFriendlyTypeName());
        }

        [Fact]
        public void ToCSharpFriendlyTypeNameSupportsNestedNullableTuples()
        {
            var actual = default((int, int, ((int, int), int), (int, int)?, int, int));
            Assert.Equal("(int, int, ((int, int), int), (int, int)?, int, int)", actual.GetType().ToCSharpFriendlyTypeName());
        }

        [Fact]
        public void ToCSharpFriendlyTypeNameSupportsTrailingNestedTuples()
        {
            var actual = default((int, int, ((int, int), int)));
            Assert.Equal("(int, int, ((int, int), int))", actual.GetType().ToCSharpFriendlyTypeName());
        }

        [Fact]
        public void ToCSharpFriendlyTypeNameSupportsTrailingNestedTuplesAtPosition8()
        {
            var actual = default((int, int, int, int, int, int, int, (int, int)));
            var alt = default((int, int, int, int, int, int, int, int, int));
            Assert.False(actual.GetType() == alt.GetType()); //non-obvious compiler-guarranteed precondition
            Assert.Equal("(int, int, int, int, int, int, int, (int, int))", actual.GetType().ToCSharpFriendlyTypeName());
        }

        [Fact]
        public void ComplexObjectToPseudoCodeSupportsTuples()
        {
            var actual = (1, "2", new[] { 1, 2, 3 });
            Assert.Equal("(1, \"2\", new[] { 1, 2, 3 })", ObjectToCode.ComplexObjectToPseudoCode(actual));
        }

        [Fact]
        public void ComplexObjectToPseudoCodeSupportsComplexTuples()
        {
            var actual = (1, ("2", new[] { "2b" }), new[] { 3 }, 4, default(string), 6, 7, 8, 9);
            Assert.Equal("(1, (\"2\", new[] { \"2b\" }), new[] { 3 }, 4, null, 6, 7, 8, 9)", ObjectToCode.ComplexObjectToPseudoCode(actual));
        }

        [Fact]
        public void ComplexObjectToPseudoCodeSupportsTuplesWithTrailingTuples()
        {
            var actual = (1, 2, 3, 4, 5, 6, 7, (8, 9, 10));
            Assert.Equal("(1, 2, 3, 4, 5, 6, 7, (8, 9, 10))", ObjectToCode.ComplexObjectToPseudoCode(actual));
        }
    }
}
