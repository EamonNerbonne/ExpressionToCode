using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExpressionToCodeLib;
using Xunit;

namespace ExpressionToCodeTest
{
    public class EnumerableFormattingTest
    {
        [Fact]
        public void ShortArraysRenderedCompletely() { Assert.Equal("new[] {1, 2, 3}", ObjectToCode.ComplexObjectToPseudoCode(new[] { 1, 2, 3 })); }

        [Fact]
        public void ShortEnumerablesRenderedCompletely() { Assert.Equal("{1, 2, 3}", ObjectToCode.ComplexObjectToPseudoCode(Enumerable.Range(1, 3))); }
        [Fact]
        public void LongEnumerablesBreakAfter10()
        { Assert.Equal("{1, 2, 3, 4, 5, 6, 7, 8, 9, 10, ...}", ObjectToCode.ComplexObjectToPseudoCode(Enumerable.Range(1, 13))); }
        [Fact]
        public void LongArraysBreakAfter10()
        { Assert.Equal("new[] {1, 2, 3, 4, 5, 6, 7, 8, 9, 10, ...}", ObjectToCode.ComplexObjectToPseudoCode(Enumerable.Range(1, 13).ToArray())); }
    }
}
